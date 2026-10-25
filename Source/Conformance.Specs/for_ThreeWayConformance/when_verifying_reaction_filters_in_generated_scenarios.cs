// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_verifying_reaction_filters_in_generated_scenarios : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var model = an_observer_filter_model.Create();

        return CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("ObserverFilters", "ObserverFilters"));
    }

    [Fact]
    async Task should_exclude_wrong_sources_streams_and_unrouted_inputs()
    {
        try
        {
            AddGeneratedSpecification("ObserverFilterProbe.cs", """
                using Cratis.Chronicle.Events;
                using Cratis.Chronicle.Testing.Reactors;
                using ObserverFilters.M.F.S;
                using Xunit;

                namespace ObserverFilters.FilterProbe;

                public class ObserverFilterProbe
                {
                    [Fact]
                    public async Task should_apply_filters_without_routing_output()
                    {
                        using var scenario = new ReactorScenario<Follow>();
                        await scenario.Given.ForEventSource("one").OnStream("Other", "Notes", "period").Events(new Recorded());
                        Assert.Empty(scenario.Produced);
                        await scenario.Given.ForEventSource("one").Events(new Recorded());
                        Assert.Empty(scenario.Produced);
                        await scenario.Given.ForEventSource("one").OnStream("Account", "Archive", "period").Events(new Recorded());
                        Assert.Empty(scenario.Produced);
                        await scenario.Given.ForEventSource("one").OnStream("Account", "Notes", "first").Events(new Recorded());
                        await scenario.Given.ForEventSource("one").OnStream("Account", "Notes", "second").Events(new Recorded());
                        Assert.Equal(2, scenario.Produced.Count);
                        Assert.All(scenario.ProducedEvents, fact =>
                        {
                            Assert.IsType<Followed>(fact.Event);
                            Assert.Equal((EventSourceId)"one", fact.EventSourceId);
                            Assert.Equal(EventSourceType.Default, fact.EventSourceType);
                            Assert.Equal(EventStreamType.All, fact.EventStreamType);
                            Assert.Equal(EventStreamId.Default, fact.EventStreamId);
                        });
                    }
                }
                """);
            var build = await Run("observer-filters-build.log", "build", "ObserverFilters.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var test = await Run("observer-filters-probe.log", "test", "ObserverFilters.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", "FullyQualifiedName~ObserverFilterProbe.should_apply_filters_without_routing_output", "--nologo");
            test.ShouldHavePassedSpecifications(1);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
