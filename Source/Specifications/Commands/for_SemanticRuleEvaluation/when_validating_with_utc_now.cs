// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation;

public class when_validating_with_utc_now : Specification
{
    static readonly DateTimeOffset _instant = new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("==", false, true, false)]
    [InlineData("!=", true, false, true)]
    [InlineData(">", false, false, true)]
    [InlineData(">=", false, true, true)]
    [InlineData("<", true, false, false)]
    [InlineData("<=", true, true, false)]
    public void should_match_reference_validation_by_instant(string operation, bool before, bool equal, bool after)
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
    [InlineData("now + 1 minute", 60)]
    [InlineData("now - 1 minute", -60)]
    [InlineData("now + 1 hour", 3600)]
    [InlineData("now - 1 hour", -3600)]
    [InlineData("now + 1 day", 86400)]
    [InlineData("now - 1 day", -86400)]
    public void should_apply_elapsed_offsets_to_property_rules(string clock, int seconds) =>
        Check(Compile("==", clock: clock), _instant.AddSeconds(seconds).UtcDateTime.ToString("O", CultureInfo.InvariantCulture), true);

    [Fact]
    public void should_preserve_authored_messages_and_warning_severity() =>
        Check(Compile(">", suffix: "severity warning message \"Choose a future instant.\""), "2026-01-02T00:00:00.0000000Z", false);

    [Fact]
    public void should_not_substitute_a_clock_when_no_occurrence_was_authored()
    {
        var plan = Compile(">");
        var command = plan.Commands.Values.Single();
        var specification = Specification(command, "2026-01-02T00:00:00Z") with { GivenClock = null };
        Assert.Throws<InvalidSemanticContract>(() => SemanticRuleEvaluation.Failures(command, specification, plan.Model.Application).ToArray()).Message.ShouldEqual(SemanticUtcNow.MissingOccurrence);
    }

    static void Check(SemanticExecutionPlan plan, string at, bool expected)
    {
        var command = plan.Commands.Values.Single();
        var specification = Specification(command, at);
        var failures = SemanticRuleEvaluation.Failures(command, specification, plan.Model.Application).ToArray();
        var request = SemanticExecutionRequest.Create(command.Id, specification.When!.Values, []) with { Occurrence = new(_instant, "caller", "Caller", "caller") };
        var reference = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request);
        failures.Length.ShouldEqual(expected ? 0 : 1);
        if (reference is SemanticRejected rejected)
        {
            Assert.False(rejected.ValidationFailures.IsEmpty, $"{rejected.Category}: {rejected.Details}");
            failures.Single().Message.ShouldEqual(rejected.ValidationFailures.Single().Message);
            failures.Single().Severity.ShouldEqual(rejected.ValidationFailures.Single().Severity);
        }
        else
        {
            reference.ShouldBeOfExactType<SemanticAccepted>();
            if (!expected) failures.Single().Severity.ShouldEqual(SemanticValidationSeverity.Warning);
        }
    }

    static SemanticSpecification Specification(SemanticCommand command, string at) =>
        new(default, "Clock", [], [], new(command.Id, [.. command.Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Text(property.Name == "id" ? "window" : at)))]), [], [], [], [])
        {
            GivenClock = _instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
        };

    static SemanticExecutionPlan Compile(string operation, bool concept = false, string clock = "now", string suffix = "")
    {
        var source = $$"""
            concept Moment : DateTime
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    at {{(concept ? "Moment" : "DateTime")}}
                    validate
                      at {{operation}} {{clock}} {{suffix}}
                    produces Kept
                      for id
                  event Kept
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ClockValidation"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", source);
        var compilation = new SemanticModelCompiler().Compile("ClockValidation", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        return SemanticExecutionPlan.Compile(compilation.Value.Model).Plan!;
    }
}
