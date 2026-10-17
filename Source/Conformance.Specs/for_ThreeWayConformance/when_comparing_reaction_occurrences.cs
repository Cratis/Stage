// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_reaction_occurrences : a_reaction_plan
{
    const string Instant = "2027-03-04T08:00:00.1230000Z";
    const string Clock = "given clock \"2027-03-04T10:00:00.123+02:00\"\n";
    const string Noted = "then Noted\n  for \"one\"\n  notedAt = \"" + Instant + "\"";
    const string Cascade = "event Finished\n  finishedAt DateTime\nreaction Finisher\n  when Noted\n    produces Finished\n      finishedAt = $context.occurred";

    [Theory]
    [InlineData("command", false)]
    [InlineData("append", false)]
    [InlineData("redelivery", false)]
    [InlineData("command", true)]
    [InlineData("append", true)]
    [InlineData("redelivery", true)]
    public async Task should_preserve_the_authored_occurrence_through_productions_and_invocations(string action, bool invoke)
    {
        var effect = invoke ? "invokes Record\n  item = item\n  seen = $context.occurred" : "produces Noted\n  notedAt = $context.occurred";
        var declaration = invoke ? "command Record\n  item String identifier\n  seen DateTime\n  produces Recorded\n    for item\n    seen = seen\n    stamped = $context.occurred\nevent Recorded\n  seen DateTime\n  stamped DateTime" : Cascade;
        var outcome = invoke ? $"then Recorded\n  for \"one\"\n  seen = \"{Instant}\"\n  stamped = \"{Instant}\"" : Noted + $"\nthen Finished\n  for \"one\"\n  finishedAt = \"{Instant}\"";
        Compile(
            "reaction Worker\n  when Started\n" + Indent(effect, 4),
            CommandEvents(action) + outcome,
            new(Action: Clock + Action(action), Declarations: declaration));
        await Execute();
        AssertMatchesReference();
        AssertExactFacts();
        _plan.Specifications.Values.Single().GivenClock.ShouldEqual(Instant);
    }

    [Theory]
    [InlineData("command")]
    [InlineData("append")]
    [InlineData("redelivery")]
    public async Task should_preserve_occurrence_in_the_first_refusal_branch_and_skip_later_invocations(string action)
    {
        Compile("""
            reaction Worker
              when Started
                invokes Complete
                  item = ""
                  on refused by validation
                    produces Noted
                      notedAt = $context.occurred
                  on refused
                    produces Followed
                      item = "wrong branch"
                invokes Complete
                  item = "one"
            """,
            CommandEvents(action) + Noted + $"\nthen Finished\n  for \"one\"\n  finishedAt = \"{Instant}\"",
            new(Action: Clock + Action(action), Authorization: "validate\n  item not empty message \"required\"", Declarations: Cascade));
        await Execute();
        AssertMatchesReference();
        AssertExactFacts();
    }

    [Theory]
    [InlineData("append")]
    [InlineData("redelivery")]
    public async Task should_match_missing_occurrence_invocation_rejection_without_manufacturing_time(string action)
    {
        Compile("reaction Worker\n  when Started\n    invokes Record\n      item = item\n      seen = $context.occurred",
            "then error \"An execution expression is unresolved in its declared root scope.\"",
            new(Action: Action(action), Declarations: "command Record\n  item String identifier\n  seen DateTime\n  produces Noted\n    for item\n    notedAt = seen"));
        await Execute();
        AssertMatchesReference();
        _result.Trace!.Rejection.ShouldEqual(((SemanticRejected)_reference.Execution).Details);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_filter_the_trigger_before_evaluating_a_missing_occurrence(bool invoke)
    {
        var effect = invoke ? "invokes Record\n  item = item\n  seen = $context.occurred" : "produces Noted\n  notedAt = $context.occurred";
        Compile(
            "reaction Worker\n  where amount < 0\n  when Started\n" + Indent(effect, 4),
            "then no events",
            new(Declarations: "command Record\n  item String identifier\n  seen DateTime\n  produces Noted\n    for item\n    notedAt = seen"));
        await Execute();
        AssertMatchesReference();
        AssertExactFacts();
    }

    [Theory]
    [InlineData("append")]
    [InlineData("redelivery")]
    public async Task should_not_offer_a_missing_clock_refusal_production_to_a_later_branch(string action)
    {
        Compile("""
            reaction Worker
              when Started
                invokes Complete
                  item = ""
                  on refused by validation
                    produces Noted
                      notedAt = $context.occurred
                  on refused
                    produces Followed
                      item = "wrong branch"
            """,
            "then error \"Reaction 'Worker' uses $context, which needs the scenario's 'given clock'.\"",
            new(Action: Action(action), Authorization: "validate\n  item not empty message \"required\""));
        await Execute();
        AssertMatchesReference();
        _result.Trace!.Rejection.ShouldEqual(((SemanticRejected)_reference.Execution).Details);
    }

    void AssertExactFacts()
    {
        var reference = ((SemanticAccepted)_reference.Execution).Facts;
        var actual = _result.Trace!.Facts;
        actual.Count.ShouldEqual(reference.Length);
        for (var index = 0; index < reference.Length; index++)
        {
            actual[index].EventContract.ShouldEqual(reference[index].EventContract.ToString());
            actual[index].EventSource.ShouldEqual(Canonical(reference[index].Destination));
            actual[index].Values.Count.ShouldEqual(reference[index].Values.Length);
            foreach (var value in reference[index].Values) actual[index].Values[value.TargetProperty.ToString()].ShouldEqual(Canonical(value.Value));
        }
    }

    static string Canonical(SemanticValue value) => value is SemanticTextValue text
        ? JsonSerializer.Serialize(text.Value) : JsonSerializer.Serialize(((SemanticNumberValue)value).Value);

    static string Action(string action) => action switch
    {
        "command" => "when Begin\n  item = \"one\"\n  amount = 1",
        "redelivery" => "given Started\n  for \"one\"\n  item = \"one\"\n  amount = 1\nwhen redelivered Started to Worker",
        _ => "when append Started\n  for \"one\"\n  item = \"one\"\n  amount = 1"
    };

    static string CommandEvents(string action) => action == "command" ? "then Started\n  for \"one\"\n  item = \"one\"\n  amount = 1\n" : string.Empty;

    static string Indent(string source, int spaces) => string.Join('\n', source.Split('\n').Select(line => new string(' ', spaces) + line));
}
