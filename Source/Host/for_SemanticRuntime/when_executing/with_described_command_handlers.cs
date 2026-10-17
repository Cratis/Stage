// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_described_command_handlers : Specification
{
    readonly DefaultHttpContext _http = new();
    IAppendSemanticFacts _appender = null!;
    ISemanticRuntime _runtime = null!;
    SemanticCommand _command = null!;
    CommandResult _denied = null!;
    CommandResult _invalid = null!;
    CommandResult _body = null!;

    void Establish()
    {
        var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(DescribedCommandHandlersCorpus.V23.EsmBytes.AsSpan()), []).Plan!;
        _command = plan.Commands.Values.Single(command => command.Handler is not null);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        _appender = appender;
        _runtime = SemanticRuntimeHosting.Create(plan, appender);
    }

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Role, "Accountant")], "specification"));
        _denied = await Execute(new ClaimsPrincipal(), 3);
        _invalid = await Execute(principal, 0);
        _body = await Execute(principal, 3);
    }

    [Fact] void should_deny_before_reaching_the_body() => _denied.IsAuthorized.ShouldBeFalse();
    [Fact] void should_validate_before_reaching_the_body() => _invalid.ValidationResults.Single().Message.ShouldEqual("Invoice must be overdue");
    [Fact] void should_report_the_reached_body() => _body.ExceptionMessages.Single().ShouldContain("Unsupported(ImplementationBody)");
    [Fact] void should_set_the_capability_header() => _http.Response.Headers["Stage-Unsupported-Capability"].ToString().ShouldEqual("ImplementationBody");
    [Fact] void should_set_the_artifact_header() => _http.Response.Headers["Stage-Unsupported-Artifact"].ToString().ShouldEqual(_command.Name);
    [Fact] void should_preserve_the_reference_message_without_a_stage_prefix() => _body.ExceptionMessages.Single().ShouldContain($"Command 'ApplyLateFee' is described but not implemented (requirement {_command.Handler!.RequirementId}); its handler body requires a target.");
    [Fact] async Task should_never_append_handler_facts() => await _appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());

    async Task<CommandResult> Execute(ClaimsPrincipal principal, int days)
    {
        var payload = new Dictionary<string, JsonElement> { ["invoiceId"] = JsonSerializer.SerializeToElement("invoice-1"), ["days"] = JsonSerializer.SerializeToElement(days) };
        var result = await _runtime.Execute(_command, payload, principal, new(DateTimeOffset.UtcNow, "subject", "name", "user"), false);

        return SemanticCommandOutcome.Map(result, CorrelationId.New(), _http, _command.Name);
    }

    void Destroy() => ((IDisposable)_runtime).Dispose();
}
