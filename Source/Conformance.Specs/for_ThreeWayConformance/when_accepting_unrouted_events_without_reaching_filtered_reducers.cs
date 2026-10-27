// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Semantics;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_accepting_unrouted_events_without_reaching_filtered_reducers
{
    [Theory]
    [InlineData("OrdersSource")]
    [InlineData("OrdersSource.Entries")]
    public async Task should_render_the_filter_and_accept_unrouted_events_without_reaching_filtered_reducers(string filter)
    {
        const string specification = """
              specification RecordingAnOrder
                when PlaceOrder
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 20
                then OrderPlaced
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 20
        """;
        var source = when_rendering_reducer_observer_filters.Source(filter).Replace("    slice StateView Totals", specification + "\n    slice StateView Totals", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var plan = SemanticExecutionPlan.Compile(loaded.Model).Plan!;
        var rendered = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.True(rendered.Success, string.Join(Environment.NewLine, rendered.Diagnostics));
        Assert.Empty(SemanticRunAdmission.ModelFeatures(loaded.Model));
        var report = await new SemanticSpecificationExecutor().Run(plan, new([.. plan.Specifications.Keys]), new());
        var result = Assert.Single(report.Results);
        Assert.Equal(SemanticSpecificationOutcome.Passed, result.Outcome);
    }
}
