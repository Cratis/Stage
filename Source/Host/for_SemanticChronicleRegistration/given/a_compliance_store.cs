// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

using AppendResult = Cratis.Chronicle.Contracts.Commands.CommandResult<Cratis.Chronicle.Contracts.Sequences.AppendManyResponse>;
using CommandResult = Cratis.Chronicle.Contracts.Commands.CommandResult;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration.given;

public class a_compliance_store : Specification
{
    internal const string Source = """
        concept PersonalName : String pii
        concept PersonalNumber : Int pii
        concept AccessToken : String secret
        module Records
          feature Registration
            slice StateChange Register
              command Register
                id String identifier
                name PersonalName
                number PersonalNumber
                token AccessToken
                produces Registered
                  for id
                  name = name
                  number = number
                  token = token
              event Registered
                name PersonalName
                number PersonalNumber
                token AccessToken
            slice StateView Summary
              readmodel Summary
                recordId String
                name PersonalName
                number PersonalNumber
                token AccessToken
              projection SummaryProjection => Summary
                from Registered key $eventSourceId
                  recordId = $eventSourceId
                  name = name
                  number = number
                  token = token
              query All => Summary[]
        """;

    protected IServices _contracts = null!;
    protected IChronicleClient _client = null!;
    protected SemanticExecutionPlan _plan = null!;
    protected ISemanticRuntime _runtime = null!;
    protected readonly List<RegisterEventTypesRequest> _registrations = [];
    protected readonly List<AppendManyForEventSourcesRequest> _appends = [];

    void Establish()
    {
        _plan = compiled_plan.From(Source);
        _contracts = Substitute.For<IServices>();
        _contracts.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(CommandResult.Success(Guid.Empty));
        _contracts.EventTypes.RegisterEventTypes(Arg.Do<RegisterEventTypesRequest>(_registrations.Add)).Returns(CommandResult.Success(Guid.Empty));
        _contracts.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new() { SequenceNumber = ulong.MaxValue }));
        _contracts.Sequences.AppendManyForEventSources(Arg.Do<AppendManyForEventSourcesRequest>(_appends.Add)).Returns(AppendResult.Success(Guid.Empty, new() { IsSuccess = true }));
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance));
        ((IChronicleServicesAccessor)connection).Services.Returns(_contracts);
        var store = Substitute.For<IEventStore>();
        store.Name.Returns(new EventStoreName("Compliance"));
        store.Connection.Returns(connection);
        _client = Substitute.For<IChronicleClient>();
        _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(store);
        _runtime = SemanticRuntimeHosting.Create(_plan, new SemanticFactAppender(_client, new StageEventStoreName("Compliance"), _plan));
    }
}
