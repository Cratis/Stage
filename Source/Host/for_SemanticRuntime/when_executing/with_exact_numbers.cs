// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_exact_numbers : Specification
{
    ISemanticRuntime _runtime = null!;
    readonly List<SemanticExecutionResult> _results = [];
    readonly List<SemanticFact> _facts = [];

    void Establish()
    {
        var source = NumberLiteralsCorpus.SourceForms.Single(form => form.Name == "single").Documents.Single().Text;
        var plan = compiled_plan.From(source + "\n" + """
                slice StateView Lookup
                  readmodel RecordedNumbers
                    id RecordId
                    largest Int
                    smallest Int
                    whole Int
                    fraction Decimal
                  projection NumbersProjection => RecordedNumbers
                    from NumbersRecorded key $eventSourceId
                      largest = largest
                      smallest = smallest
                      whole = whole
                      fraction = fraction
                  query ById => RecordedNumbers optional
                    by id RecordId
            """);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(_ => _facts.Count == 0 ? ulong.MaxValue : (ulong)_facts.Count - 1);
        appender.Append(Arg.Do<IReadOnlyList<SemanticFact>>(_facts.AddRange), Arg.Any<SemanticCommandOccurrence>()).Returns(Task.CompletedTask);
        _runtime = SemanticRuntimeHosting.Create(plan, appender);
    }

    async Task Because()
    {
        foreach (var name in new[] { "RecordLiterals", "RecordInputs" })
        {
            var values = new Dictionary<string, JsonElement>
            {
                ["id"] = JsonSerializer.SerializeToElement(Guid.NewGuid())
            };
            if (name == "RecordInputs")
            {
                values["largest"] = JsonSerializer.SerializeToElement(9007199254740991L);
                values["smallest"] = JsonSerializer.SerializeToElement(-9007199254740991L);
                values["whole"] = JsonSerializer.SerializeToElement(1234567890123456L);
                values["fraction"] = JsonSerializer.SerializeToElement(0.10000000000000002m);
            }
            _results.Add(await _runtime.Execute(
                _runtime.Plan.Commands.Values.Single(command => command.Name == name),
                values,
                new ClaimsPrincipal(),
                new(DateTimeOffset.UtcNow, "subject", "name", "user"),
                false));
        }
    }

    [Fact] void should_admit_v11() => _runtime.Plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V11);
    [Fact] void should_accept_both_commands() => _results.Select(result => result.Kind).ShouldContainOnly(SemanticExecutionOutcomeKind.Accepted, SemanticExecutionOutcomeKind.Accepted);
    [Fact] void should_append_both_exact_facts() => _facts.Count.ShouldEqual(2);
    [Fact] void should_preserve_every_literal_and_input() => _facts.SelectMany(fact => fact.Values).Select(value => ((SemanticNumberValue)value.Value).Value).ShouldContainOnly(9007199254740991m, -9007199254740991m, 1234567890123456m, 0.10000000000000002m, 9007199254740991m, -9007199254740991m, 1234567890123456m, 0.10000000000000002m);
    [Fact] async Task should_materialize_both_instances() => (await _runtime.ReadModels(_runtime.Plan.ReadModels.Keys.Single())).Length.ShouldEqual(2);
    [Fact] async Task should_project_both_facts_without_rounding() => (await ProjectedNumbers()).ShouldContainOnly(9007199254740991m, -9007199254740991m, 1234567890123456m, 0.10000000000000002m, 9007199254740991m, -9007199254740991m, 1234567890123456m, 0.10000000000000002m);

    async Task<decimal[]> ProjectedNumbers()
    {
        var model = _runtime.Plan.ReadModels.Values.Single();
        var instances = await _runtime.ReadModels(model.Id);

        return [.. instances.SelectMany(instance => instance.Values)
            .Where(value => !model.Properties.Single(property => property.Id == value.TargetProperty).IsIdentifier)
            .Select(value => ((SemanticNumberValue)value.Value).Value)];
    }
}
