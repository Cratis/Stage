// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

using CommandResult = Cratis.Chronicle.Contracts.Commands.CommandResult<Cratis.Chronicle.Contracts.Sequences.AppendManyResponse>;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_an_external_writer_arrives_during_append : a_semantic_runtime
{
    readonly List<AppendManyForEventSourcesRequest> _requests = [];
    SemanticExecutionResult _first = null!;
    SemanticExecutionResult _second = null!;
    ISemanticRuntime _chronicleRuntime = null!;
    ulong _tail = ulong.MaxValue;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var services = Substitute.For<IServices>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        services.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(_ =>
            QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new() { SequenceNumber = _tail }));
        services.Sequences.AppendManyForEventSources(Arg.Any<AppendManyForEventSourcesRequest>()).Returns(call =>
        {
            var request = call.Arg<AppendManyForEventSourcesRequest>();
            _requests.Add(request);
            if (_requests.Count == 1)
            {
                _tail = 0;
                return CommandResult.Success(Guid.Empty, new() { IsSuccess = true, ConcurrencyCheckPerformed = true });
            }

            // The external writer appended after the preflight read, before Chronicle validates this request.
            _tail = 1;
            return CommandResult.Success(Guid.Empty, new()
            {
                HasConcurrencyViolations = true,
                ConcurrencyCheckPerformed = true,
                ConcurrencyViolations = [new() { ActualSequenceNumber = 1, ExpectedSequenceNumber = 0 }]
            });
        });
        var store = Substitute.For<IEventStore>();
        store.Name.Returns(new EventStoreName("stage-specs"));
        store.Connection.Returns(connection);
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>()).Returns(store);
        var appenderType = typeof(SemanticRuntimeHosting).Assembly.GetType("Cratis.Stage.Semantics.SemanticFactAppender", throwOnError: true)!;
        var appender = (IAppendSemanticFacts)Activator.CreateInstance(appenderType, client, new StageEventStoreName("stage-specs"), _runtime.Plan)!;
        _chronicleRuntime = SemanticRuntimeHosting.Create(_runtime.Plan, appender);
    }

    async Task Because()
    {
        _first = await Execute("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        _second = await Execute("3fa85f64-5717-4562-b3fc-2c963f66afa7");
    }

    [Fact] void should_append_the_first_fact_to_the_empty_log() => _first.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_send_one_unrestricted_scope_for_the_empty_log()
    {
        var scope = _requests[0].ConcurrencyScopes!.Single();
        scope.EventSourceId.ShouldEqual("__stage:event-log-tail__");
        scope.Scope.EventSourceId.ShouldBeFalse();
        scope.Scope.EventTypes!.ShouldBeEmpty();
        scope.Scope.EventSourceType.ShouldBeNull();
        scope.Scope.EventStreamId.ShouldBeNull();
        scope.Scope.EventStreamType.ShouldBeNull();
        scope.Scope.SequenceNumber.ShouldEqual(ulong.MaxValue);
        scope.Scope.ExpectsNoMatchingEvent.ShouldBeTrue();
    }

    [Fact] void should_send_the_checked_tail_for_the_next_append()
    {
        var scope = _requests[1].ConcurrencyScopes!.Single();
        scope.EventSourceId.ShouldEqual(_requests[0].ConcurrencyScopes!.Single().EventSourceId);
        scope.Scope.EventSourceId.ShouldBeFalse();
        scope.Scope.EventTypes!.ShouldBeEmpty();
        scope.Scope.EventSourceType.ShouldBeNull();
        scope.Scope.EventStreamId.ShouldBeNull();
        scope.Scope.EventStreamType.ShouldBeNull();
        scope.Scope.SequenceNumber.ShouldEqual(0UL);
        scope.Scope.ExpectsNoMatchingEvent.ShouldBeFalse();
    }

    [Fact] void should_fault_on_the_atomic_rejection() => _second.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_identify_the_external_change() => ((ISemanticRuntimeStatus)_chronicleRuntime).FaultReason.ShouldContain("outside this session (0 -> 1)");
    [Fact] void should_not_commit_the_rejected_world() => _second.World.Facts.Length.ShouldEqual(1);

    Task<SemanticExecutionResult> Execute(string id) => _chronicleRuntime.Execute(
        _command,
        new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement(id),
            ["name"] = JsonSerializer.SerializeToElement("Screenplay")
        },
        new ClaimsPrincipal(),
        new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"),
        false);
}
