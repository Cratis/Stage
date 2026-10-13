// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_the_observer_filter_corpus(when_comparing_the_observer_filter_corpus.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_the_observer_filter_corpus.context>
{
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_pass_the_derived_routed_reaction() => fixture.Outcomes.ShouldContainOnly(["derived/RoutedReaction: Reference=Passed, Stage=Passed, Rendered=Passed"]);
    [Fact] void should_keep_history_without_reacting_to_it() => fixture.FactCount.ShouldEqual(3);

    [Theory]
    [InlineData("UnroutedReaction", "STAGE-ESM-011")]
    [InlineData("WrongStreamReaction", "STAGE-ESM-011")]
    public void should_refuse_the_remaining_corpus_boundaries_separately(string specification, string code)
    {
        var model = an_observer_filter_model.Create(specification);
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("ObserverFilters", "ObserverFilters"));
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-035");
    }

    [Fact]
    void should_refuse_the_unrendered_reducer_body_in_the_complete_corpus()
    {
        var model = SemanticModelSerializer.Deserialize(EventRoutesCorpus.ObserverFiltersV10.EsmBytes.AsSpan());
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("ObserverFilters", "ObserverFilters"));
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-027");
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-030");
    }

    public class context : a_three_way_application
    {
        public int FactCount { get; private set; }
        protected override string ApplicationName => "ObserverFilters";
        protected override string ProjectFile => "ObserverFilters.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("derived", an_observer_filter_model.Create())];

        async Task Because()
        {
            var plan = SemanticExecutionPlan.Compile(an_observer_filter_model.Create()).Plan!;
            FactCount = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single()).Execution.World.Facts.Length;
            await Verify();
        }
    }
}
#endif
