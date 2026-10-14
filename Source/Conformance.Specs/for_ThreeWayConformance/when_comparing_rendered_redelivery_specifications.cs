// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_rendered_redelivery_specifications(when_comparing_rendered_redelivery_specifications.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_rendered_redelivery_specifications.context>
{
    [Fact] void should_execute_every_redelivery_case() => fixture.Outcomes.Length.ShouldEqual(8);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_pass_named_only_nonzero_history_filtered_and_invoked_cases() => fixture.Outcomes.Count(outcome => outcome.EndsWith("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(6);
    [Fact] void should_fail_wrong_values_and_nonempty_no_events_expectations() => fixture.Outcomes.Count(outcome => outcome.EndsWith("Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal)).ShouldEqual(2);

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
                  event OtherObserved
                    id String
                  reaction Worker
                    from Account.Notes
                    when Started
                      produces Followed
                        id = id
                    where amount > 0
                  reaction Sibling
                    when Started
                      produces OtherObserved
                        id = id
                  specification NamedOnly
                    given OtherObserved
                      for "earlier"
                      id = "earlier"
                    given Started
                      for "selected-source"
                      stream Account.Notes
                      id = "selected-value"
                      amount = 1
                    when redelivered Started to Worker
                    then Followed
                      for "selected-source"
                      id = "selected-value"
                  specification WrongValue
                    given Started
                      for "one"
                      stream Account.Notes
                      id = "one"
                      amount = 1
                    when redelivered Started to Worker
                    then Followed
                      id = "wrong"
                  specification NotEmpty
                    given Started
                      for "one"
                      stream Account.Notes
                      id = "one"
                      amount = 1
                    when redelivered Started to Worker
                    then no events
                  specification EmptyWhere
                    given Started
                      for "one"
                      stream Account.Notes
                      id = "one"
                      amount = 0
                    when redelivered Started to Worker
                    then no events
                  specification WrongStream
                    given Started
                      for "one"
                      stream Account.Archive
                      id = "one"
                      amount = 1
                    when redelivered Started to Worker
                    then no events
                  specification WrongSource
                    given Started
                      for "one"
                      stream Other.Notes
                      id = "one"
                      amount = 1
                    when redelivered Started to Worker
                    then no events
                  specification Unrouted
                    given Started
                      for "one"
                      id = "one"
                      amount = 1
                    when redelivered Started to Worker
                    then no events
                slice Automation Invoke
                  command RequestWork
                    id String identifier
                    produces Requested
                      for id
                      id = id
                  command Complete
                    id String identifier
                    produces Completed
                      for id
                      id = id
                  event Requested
                    id String
                  event Completed
                    id String
                  reaction Completer
                    when Requested
                      invokes Complete
                        id = id
                  specification Invoked
                    given Requested
                      for "one"
                      id = "one"
                    when redelivered Requested to Completer
                    then Completed
                      id = "one"
            """;
    }
}
#endif
