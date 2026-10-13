// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_filtered_reaction_specifications(when_comparing_filtered_reaction_specifications.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_filtered_reaction_specifications.context>
{
    [Fact] void should_compare_source_stream_identity_and_sibling_selection() => fixture.Outcomes.Length.ShouldEqual(4);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_pass_every_rendered_specification() => fixture.Outcomes.All(outcome => outcome.EndsWith("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_three_way_application
    {
        const string Source = """
            policy CanComplete
              require role "Automation"
            eventsource Account
              id "stored-account"
              identifier String
              stream Notes
                id "stored-notes"
                streamId Uuid
              stream Archive
                id "stored-archive"
            module Work
              feature Processing
                slice Automation FollowUp
                  command Complete
                    id String identifier
                    authorize CanComplete
                    produces Completed
                      for id
                  event Started
                    id String
                  event Recorded
                    id String
                  event Requested
                    id String
                  event Shared
                    id String
                  event Followed
                    id String
                  event Completed
                  reaction FollowAny
                    from Account
                    when Started
                      produces Followed
                        for id
                        id = id
                  reaction FollowNotes
                    from Account.Notes
                    when Recorded
                      produces Followed
                        for id
                        id = id
                  reaction Invoke
                    runs as system role "Automation"
                    from Account
                    when Requested
                      invokes Complete
                        id = id
                  reaction FilteredSibling
                    from Account.Notes
                    when Shared
                      produces Followed
                        for id
                        id = "excluded"
                  reaction UnfilteredSibling
                    when Shared
                      produces Followed
                        for id
                        id = id
                  specification SourceOnly
                    given Started
                      for "history"
                      stream Account.Archive
                      id = "history"
                    when append Started
                      for "source-only"
                      stream Account.Archive
                      id = "source-only"
                    then Followed
                      for "source-only"
                      no stream
                      id = "source-only"
                  specification SourceAndStream
                    when append Recorded
                      for "source-stream"
                      stream Account.Notes
                        streamId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      id = "source-stream"
                    then Followed
                      for "source-stream"
                      no stream
                      id = "source-stream"
                  specification SystemInvoker
                    when append Requested
                      for "invoker"
                      stream Account.Archive
                      id = "invoker"
                    then Completed
                      for "invoker"
                      no stream
                  specification FilteredSibling
                    when append Shared
                      for "sibling"
                      stream Account.Archive
                      id = "sibling"
                    then Followed
                      for "sibling"
                      no stream
                      id = "sibling"
            """;

        protected override string ApplicationName => "FilteredReactions";
        protected override string ProjectFile => "FilteredReactions.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("inline", an_automation.Compile(Source))];
        Task Because() => Verify();
    }
}
#endif
