// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_context_metadata : a_semantic_runtime
{
    readonly DateTimeOffset _occurred = new(2026, 9, 24, 12, 30, 0, TimeSpan.Zero);
    SemanticExecutionResult _result = null!;
    SemanticFact _fact = null!;
    SemanticCommandOccurrence _appendedOccurrence = null!;

    async Task Because()
    {
        ((IAppendSemanticFacts)_appender).Append(
            Arg.Do<IReadOnlyList<SemanticFact>>(facts => _fact = facts.Single()),
            Arg.Do<SemanticCommandOccurrence>(occurrence => _appendedOccurrence = occurrence)).Returns(Task.CompletedTask);
        _result = await _runtime.Execute(
            _command,
            new Dictionary<string, JsonElement>
            {
                ["projectId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                ["name"] = JsonSerializer.SerializeToElement("Screenplay")
            },
            new ClaimsPrincipal(),
            new(_occurred, "caller", "Caller", "caller-name"),
            false);
    }

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_the_same_occurrence_time() => _appendedOccurrence.Occurred.ShouldEqual(_occurred);
    [Fact] void should_append_the_same_caller() => _appendedOccurrence.Subject.ShouldEqual("caller");
    [Fact] void should_project_occurred_from_the_appended_occurrence() => DateTimeOffset.Parse(Value("registeredAt")!).ShouldEqual(_appendedOccurrence.Occurred);
    [Fact] void should_project_caused_by_from_the_appended_occurrence() => Value("registeredBy").ShouldEqual(_appendedOccurrence.Subject);

    string? Value(string name)
    {
        var property = _runtime.Plan.Events[_fact.EventContract].Properties.Single(candidate => candidate.Name == name);
        return (_fact.Values.Single(candidate => candidate.TargetProperty == property.Id).Value as SemanticTextValue)?.Value;
    }
}
