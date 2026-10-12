// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_concept_compliance : Specification
{
    ISemanticRuntime _runtime = null!;
    SemanticExecutionResult _result = null!;
    SemanticWorld _rebuilt = null!;
    readonly List<SemanticFact> _facts = [];

    void Establish()
    {
        var source = ConceptComplianceCorpus.V13.SourceForms.Single(form => form.Name == "single").Documents.Single().Text;
        var plan = compiled_plan.From(source);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(_ => _facts.Count == 0 ? ulong.MaxValue : (ulong)_facts.Count - 1);
        appender.Append(Arg.Do<IReadOnlyList<SemanticFact>>(_facts.AddRange), Arg.Any<SemanticCommandOccurrence>()).Returns(Task.CompletedTask);
        _runtime = SemanticRuntimeHosting.Create(plan, appender);
    }

    async Task Because()
    {
        var values = new Dictionary<string, JsonElement>
        {
            ["id"] = JsonSerializer.SerializeToElement(ConceptComplianceCorpus.V13.RuntimeStreamId),
            ["health"] = JsonSerializer.SerializeToElement("well"),
            ["key"] = JsonSerializer.SerializeToElement("token"),
            ["note"] = JsonSerializer.SerializeToElement("private"),
            ["status"] = JsonSerializer.SerializeToElement("draft")
        };
        _result = await _runtime.Execute(_runtime.Plan.Commands.Values.Single(), values, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "subject", "name", "user"), false);
        var stored = new AppendedEventResponse
        {
            Context = new()
            {
                EventType = new() { Id = _runtime.Plan.Events.Values.Single().Name, Generation = 1 },
                EventSourceId = ConceptComplianceCorpus.V13.RuntimeStreamId,
                SequenceNumber = 0,
                EventSourceType = "Default",
                EventStreamType = "All",
                EventStreamId = "Default",
                Occurred = new() { Value = "2026-10-12T12:00:00.0000000+00:00" }
            },
            Content = """{"health":"well","key":"token","note":"private","status":-1}"""
        };
        _rebuilt = SemanticWorldRebuilder.Create(_runtime.Plan, [stored], 0);
    }

    [Fact] void should_admit_v13() => _runtime.Plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V13);
    [Fact] void should_accept_the_command() => _result.Kind.ShouldEqual(SemanticExecutionOutcomeKind.Accepted);
    [Fact] void should_preserve_protected_values_on_append() => _facts.Single().Values.Select(value => ((SemanticTextValue)value.Value).Value).ShouldContainOnly("well", "token", "private", "draft");
    [Fact] void should_preserve_protected_values_after_rebuild() => _rebuilt.Facts.Single().Values.Select(value => ((SemanticTextValue)value.Value).Value).ShouldContainOnly("well", "token", "private", "draft");
}
