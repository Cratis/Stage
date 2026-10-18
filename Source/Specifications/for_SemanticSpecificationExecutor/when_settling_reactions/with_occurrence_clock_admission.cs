// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_occurrence_clock_admission : given.a_reaction_plan
{
    void Establish() => Compile(
        "reaction Worker\n  when Started\n    produces Noted\n      notedAt = $context.occurred",
        "then Noted\n  notedAt = \"2027-03-04T08:00:00.1230000Z\"",
        new(Action: "given clock \"2027-03-04T10:00:00.123+02:00\"\nwhen append Started\n  for \"one\"\n  item = \"one\"\n  amount = 1"));

    [Theory]
    [InlineData("command")]
    [InlineData("append")]
    [InlineData("redelivery")]
    [InlineData("trigger")]
    public void should_admit_only_the_independent_occurrence_clock_without_relaxing_renderer_clocks(string action)
    {
        var fixture = WithAction(action);
        SemanticSpecificationClock.TryGetOccurrence(fixture, out var instant).ShouldBeTrue();
        instant.ShouldEqual(new DateTimeOffset(2027, 3, 4, 8, 0, 0, 123, TimeSpan.Zero));
        SemanticRunAdmission.SpecificationFeatures(fixture).ShouldBeEmpty();
        SemanticSpecificationClock.TryGet(fixture, out _).ShouldEqual(action == "command");
    }

    [Theory]
    [InlineData("2027-03-04T08:00:00.1230001Z")]
    [InlineData("2027-03-04T08:00:00.123Z")]
    [InlineData("2027-03-04T10:00:00.1230000+02:00")]
    [InlineData("2027-03-04T08:00:00.1230000")]
    [InlineData("1969-03-04T08:00:00.1230000Z")]
    [InlineData("not a clock")]
    public void should_preserve_canonical_range_and_precision_refusals_for_every_supported_action(string clock)
    {
        foreach (var action in new[] { "command", "append", "redelivery", "trigger" })
        {
            var fixture = WithAction(action) with { GivenClock = clock };
            SemanticSpecificationClock.TryGetOccurrence(fixture, out _).ShouldBeFalse();
            SemanticRunAdmission.SpecificationFeatures(fixture).Single().Capability.ShouldEqual(nameof(StageExecutionCapability.Time));
        }
    }

    [Theory]
    [InlineData("query")]
    [InlineData("clock")]
    [InlineData("capture")]
    [InlineData("given-capture")]
    [InlineData("mixed")]
    public void should_not_admit_non_occurrence_actions_or_ambiguous_actions(string action)
    {
        var fixture = WithAction(action);
        SemanticSpecificationClock.TryGetOccurrence(fixture, out _).ShouldBeFalse();
        if (action != "mixed") SemanticRunAdmission.SpecificationFeatures(fixture).Single().Capability.ShouldEqual(nameof(StageExecutionCapability.Time));
    }

    [Theory]
    [InlineData("command")]
    [InlineData("mixed")]
    [InlineData("capture")]
    [InlineData("given-capture")]
    public void should_not_mistake_other_actions_for_automatic_actions(string action) => SemanticSpecificationClock.TryGetAutomaticAction(WithAction(action), out _, out _).ShouldBeFalse();

    [Fact]
    public void should_admit_a_trigger_without_inventing_an_occurrence()
    {
        SemanticSpecificationClock.TryGetAutomaticAction(WithAction("trigger") with { GivenClock = null }, out var from, out var to).ShouldBeTrue();
        from.ShouldBeNull();
        to.ShouldBeNull();
    }

    [Fact]
    public void should_admit_a_canonical_clock_window() => SemanticSpecificationClock.TryGetAutomaticAction(WithAction("clock") with { WhenClock = "2027-03-04T09:00:00.0000000Z" }, out _, out _).ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("2027-03-04T08:00:00.1230001Z")]
    [InlineData("2027-03-04T09:00:00Z")]
    public void should_refuse_a_clock_advance_without_a_canonical_given_clock(string? clock) => SemanticSpecificationClock.TryGetAutomaticAction(WithAction("clock") with { GivenClock = clock }, out _, out _).ShouldBeFalse();

    [Fact]
    public void should_keep_an_absent_clock_absent() => SemanticSpecificationClock.TryGetOccurrence(WithAction("append") with { GivenClock = null }, out _).ShouldBeFalse();

    SemanticSpecification WithAction(string action)
    {
        var fixture = _plan.Specifications.Values.Single();
        var append = fixture.WhenAppended;
        var when = new SemanticSpecificationCommand(_plan.Commands.Values.Single(command => command.Name == "Begin").Id, []);

        return fixture with
        {
            WhenAppended = action == "append" || action == "given-capture" || action == "mixed" ? append : null,
            When = action == "command" || action == "mixed" ? when : null,
            WhenRedelivered = action == "redelivery" ? new(_plan.Reactions.Single().Id, 0) : null,
            WhenClock = action == "clock" ? fixture.GivenClock : null,
            WhenTrigger = action == "trigger" ? new(SemanticReactionTriggerKind.ApplicationTrigger, []) : null,
            WhenCapture = action == "capture" ? new(default, new([])) : null,
            GivenCaptures = action == "given-capture" ? [new(default, new([]))] : []
        };
    }
}
