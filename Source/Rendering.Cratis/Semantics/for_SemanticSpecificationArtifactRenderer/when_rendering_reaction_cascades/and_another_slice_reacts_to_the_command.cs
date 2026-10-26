// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationArtifactRenderer.when_rendering_reaction_cascades;

/// <summary>
/// Screenplay decision 0115: a specification runs only its own slice, so a reaction another slice declares is no cascade of it.
/// </summary>
public class and_another_slice_reacts_to_the_command : given.an_inventory
{
    void Because() => Plan(SeparateSlices);

    [Fact] void should_render_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_not_refuse_any_specification() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-011");
    [Fact] void should_render_the_command_specification_once() => Paths("when_registering.cs").Count().ShouldEqual(1);
    [Fact] void should_not_split_the_command_specification_into_a_trigger() => Paths("when_registering_trigger.cs").ShouldBeEmpty();
    [Fact] void should_run_the_command_in_a_command_scenario() => Source("when_registering.cs").ShouldContain("CommandScenario");
    [Fact] void should_assert_that_the_command_succeeds() => Source("when_registering.cs").ShouldContain("void should_succeed() => _result.ShouldBeSuccessful();");
    [Fact] void should_assert_the_registered_event() => Source("when_registering.cs").ShouldContain("SkuRegistered");
    [Fact] void should_not_assert_the_snapshot_event() => Source("when_registering.cs").ShouldNotContain("StockLevelRecorded");
    [Fact] void should_render_the_snapshot_reactor() => Source("Snapshot.cs").ShouldContain("class Snapshot");
    [Fact] void should_render_the_snapshot_slices_own_specification() => Source("when_snapshotting.cs").ShouldContain("StockLevelRecorded");
}
