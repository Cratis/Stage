// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation;

public class when_comparing_with_utc_now : Specification
{
    static readonly DateTimeOffset _instant = new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("==", false, true, false)]
    [InlineData("!=", true, false, true)]
    [InlineData(">", false, false, true)]
    [InlineData(">=", false, true, true)]
    [InlineData("<", true, false, false)]
    [InlineData("<=", true, true, false)]
    public void should_compare_requirements_by_instant(string operation, bool before, bool equal, bool after)
    {
        foreach (var concept in new[] { false, true })
        {
            var plan = Compile(operation, concept);
            Check(plan, "2026-01-01T23:59:59.9999999Z", before);
            Check(plan, "2026-01-02T00:00:00.0000000Z", equal);
            Check(plan, "2026-01-02T00:00:00.0000001Z", after);
            Check(plan, "2026-01-01T19:00:00.0000000-05:00", equal);
        }
    }

    [Theory]
    [InlineData(SemanticClockUnit.Minute, 1, 60)]
    [InlineData(SemanticClockUnit.Minute, -1, -60)]
    [InlineData(SemanticClockUnit.Hour, 1, 3600)]
    [InlineData(SemanticClockUnit.Hour, -1, -3600)]
    [InlineData(SemanticClockUnit.Day, 1, 86400)]
    [InlineData(SemanticClockUnit.Day, -1, -86400)]
    public void should_apply_elapsed_offsets_to_the_supplied_instant(SemanticClockUnit unit, int amount, int seconds) =>
        SemanticUtcNow.Instant(new(SemanticClockKind.Now, amount, unit), _instant).ShouldEqual(_instant.AddSeconds(seconds));

    [Fact]
    public void should_not_substitute_a_clock_when_no_occurrence_was_authored()
    {
        var (command, specification) = Fixture(Compile("==", false), "2026-01-02T00:00:00Z");
        specification = specification with { GivenClock = null };
        var exception = Assert.Throws<InvalidSemanticContract>(() => SemanticRuleEvaluation.RequirementFailures(command, specification).ToArray());
        exception.Message.ShouldEqual(SemanticUtcNow.MissingOccurrence);
    }

    [Fact]
    public void should_leave_today_and_date_of_unsupported()
    {
        Assert.Throws<UnsupportedSemanticMapping>(() => SemanticUtcNow.Instant(new(SemanticClockKind.Today, 0, SemanticClockUnit.Day), _instant));
        Assert.Throws<UnsupportedSemanticMapping>(() => SemanticUtcNow.Instant(new(SemanticClockKind.Now, 0, SemanticClockUnit.Day) { DateOf = true }, _instant));
    }

    static void Check(SemanticExecutionPlan plan, string value, bool expected)
    {
        var (command, specification) = Fixture(plan, value);
        var failures = SemanticRuleEvaluation.RequirementFailures(command, specification).ToArray();
        failures.Length.ShouldEqual(expected ? 0 : 1);
        var request = SemanticExecutionRequest.Create(command.Id, specification.When!.Values, []) with
        {
            Occurrence = new(_instant, "caller", "Caller", "caller")
        };
        var reference = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request);
        if (expected)
        {
            reference.ShouldBeOfExactType<SemanticAccepted>();
        }
        else
        {
            var rejected = Assert.IsType<SemanticRejected>(reference);
            rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
            failures.Single().Message.ShouldEqual(rejected.ValidationFailures[0].Message);
        }
    }

    static (SemanticCommand Command, SemanticSpecification Specification) Fixture(SemanticExecutionPlan plan, string at)
    {
        var command = plan.Commands.Values.Single();
        var values = command.Properties.Select(property => new SemanticPropertyValue(property.Id,
            SemanticValue.Text(property.Name == "id" ? "window" : at))).ToImmutableArray();
        return (command, new SemanticSpecification(default, "Clock", [], [], new(command.Id, values), [], [], [], [])
        {
            GivenClock = _instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
        });
    }

    static SemanticExecutionPlan Compile(string operation, bool concept)
    {
        var source = $$"""
            concept Moment : DateTime
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    at {{(concept ? "Moment" : "DateTime")}}
                    require at {{operation}} now
                    produces Kept
                      for id
                  event Kept
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("UtcNow"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", source);
        var compilation = new SemanticModelCompiler().Compile("UtcNow", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        return SemanticExecutionPlan.Compile(compilation.Value.Model).Plan!;
    }
}
