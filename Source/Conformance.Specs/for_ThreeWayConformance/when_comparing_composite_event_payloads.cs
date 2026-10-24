// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_composite_event_payloads(when_comparing_composite_event_payloads.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_composite_event_payloads.context>
{
    [Fact] void should_cover_given_produced_and_appended_invoked_values() => fixture.Outcomes.Length.ShouldEqual(3);
    [Fact] void should_match_all_three_engines() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_execute_composite_facts() => Assert.All(fixture.Outcomes, outcome => Assert.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", outcome, StringComparison.Ordinal));

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "CompositeFacts";
        protected override string ProjectFile => "CompositeFacts.csproj";
        protected override bool RequireStageExecution => true;

        // Keep unobserved appends and reordered expectations in the executor specs: the renderer refuses them.
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("typed", when_running_composite_event_values.Compile(when_comparing_composite_event_payloads.Source).Model)];
        Task Because() => Verify();
    }

    const string Source = """
        type Child
          count Int
        type Details
          label String
          child Child
        module Facts
          feature Recording
            slice StateChange Record
              command RecordDetails
                id String identifier
                details Details
                produces DetailsRecorded
                  for id
                  details = details
              command RequestDetails
                id String identifier
                details Details
                produces DetailsRequested
                  for id
                  details = details
              event DetailsRecorded
                details Details
              specification ProducedComposite
                when RecordDetails
                  id = "one"
                  details = {"label":"nested","child":{"count":7}}
                then DetailsRecorded
                  details = {"label":"nested","child":{"count":7}}
              specification GivenComposite
                given DetailsRecorded
                  for "before"
                  details = {"label":"nested","child":{"count":6}}
                when RecordDetails
                  id = "one"
                  details = {"label":"nested","child":{"count":7}}
                then DetailsRecorded
                  details = {"label":"nested","child":{"count":7}}
            slice Automation Forward
              event DetailsRequested
                details Details
              reaction ForwardDetails
                when DetailsRequested
                  invokes RecordDetails
                    id = "one"
                    details = details
              specification InvokedComposite
                when append DetailsRequested
                  for "one"
                  details = {"label":"nested","child":{"count":7}}
                then DetailsRecorded
                  details = {"label":"nested","child":{"count":7}}
        """;
}
#endif
