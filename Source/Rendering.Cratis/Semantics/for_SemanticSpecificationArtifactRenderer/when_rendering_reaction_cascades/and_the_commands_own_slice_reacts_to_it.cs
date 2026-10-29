// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationArtifactRenderer.when_rendering_reaction_cascades;

/// <summary>
/// A reaction in the command's own slice runs in its specification, so the specification is still split into the reactor and its trigger.
/// </summary>
public class and_the_commands_own_slice_reacts_to_it : given.an_inventory
{
    void Because() => Plan(OneSlice);

    [Fact] void should_render_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_not_refuse_any_specification() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-011");
    [Fact] void should_render_the_reaction_half_through_the_reactor() => Source("when_registering.cs").ShouldContain("_reactor.Produced.OfType<");
    [Fact] void should_render_the_reaction_half_asserting_the_snapshot_event() => Source("when_registering.cs").ShouldContain("StockLevelRecorded");
    [Fact] void should_render_the_trigger_half_as_its_own_command_specification() => Source("when_registering_trigger.cs").ShouldContain("CommandScenario");
    [Fact] void should_assert_the_registered_event_in_the_trigger_half() => Source("when_registering_trigger.cs").ShouldContain("SkuRegistered");
}
