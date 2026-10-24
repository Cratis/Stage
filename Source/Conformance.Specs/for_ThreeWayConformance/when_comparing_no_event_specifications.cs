// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_no_event_specifications(when_comparing_no_event_specifications.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_no_event_specifications.context>
{
    [Fact] void should_execute_a_nonempty_acceptance_matrix() => fixture.Outcomes.Length.ShouldEqual(8);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_pass_the_response_seed_and_excluded_reactor_cases() => fixture.Outcomes.Count(outcome => outcome.EndsWith("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(6);
    [Fact] void should_fail_both_nonempty_no_events_cases() => fixture.Outcomes.Count(outcome => outcome.Contains("NotEmpty: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal)).ShouldEqual(2);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "Recovery";
        protected override string ProjectFile => "Recovery.csproj";
        protected override bool RequireStageExecution => true;
        protected override bool ExpectFailedGeneratedTest => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("inline", a_recovery_plan.Compile(Source))];
        Task Because() => Verify();

        const string Source = """
            eventsource Account
              id "account"
              identifier String
              stream Notes
                id "notes"
              stream Archive
                id "archive"
            eventsource Other
              id "other"
              identifier String
              stream Notes
                id "notes"
            module Work
              feature Items
                slice StateChange Respond
                  command Echo
                    id String identifier
                    returns id
                  command SeedPrior
                    id String identifier
                    produces Prior
                      for id
                      id = id
                  event Prior
                    id String
                  specification EmptyResponse
                    when Echo
                      id = "one"
                    then returns "one"
                    then no events
                  specification SeededResponse
                    given Prior
                      for "history"
                      stream Account.Archive
                      id = "history"
                    when Echo
                      id = "one"
                    then returns "one"
                    then no events
                slice StateChange Produce
                  command Produce
                    id String identifier
                    produces Changed
                      for id
                      id = id
                    produces Noted
                      for id
                      id = id
                  event Changed
                    id String
                  event Noted
                    id String
                  specification CommandNotEmpty
                    when Produce
                      for "one"
                      id = "one"
                    then no events
                slice Automation FollowUp
                  command Begin
                    id String identifier
                    amount Decimal
                    produces Started
                      for id
                      id = id
                      amount = amount
                  event Started
                    id String
                    amount Decimal
                  event Followed
                    id String
                  reaction Worker
                    from Account.Notes
                    when Started
                      produces Followed
                        for id
                        id = id
                    where amount > 0
                  specification EmptyWhere
                    given Started
                      for "history"
                      stream Account.Notes
                      id = "history"
                      amount = 1
                    when append Started
                      for "one"
                      stream Account.Notes
                      id = "one"
                      amount = 0
                    then no events
                  specification ReactionNotEmpty
                    when append Started
                      for "one"
                      stream Account.Notes
                      id = "one"
                      amount = 1
                    then no events
                  specification WrongStream
                    when append Started
                      for "one"
                      stream Account.Archive
                      id = "one"
                      amount = 1
                    then no events
                  specification WrongSource
                    when append Started
                      for "one"
                      stream Other.Notes
                      id = "one"
                      amount = 1
                    then no events
                  specification Unrouted
                    when append Started
                      for "one"
                      id = "one"
                      amount = 1
                    then no events
            """;
    }
}
#endif
