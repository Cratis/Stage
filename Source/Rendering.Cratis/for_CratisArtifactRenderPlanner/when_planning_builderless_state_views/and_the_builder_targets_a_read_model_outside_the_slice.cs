// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_builderless_state_views;

public class and_the_builder_targets_a_read_model_outside_the_slice : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = builderless_state_view_model.BuiltQuery.Replace("        id Uuid\n        text String\n      query", "        id Uuid key\n        text String\n      query", StringComparison.Ordinal);
        source.ShouldContain("slice StateView WelcomeInfo\n      readmodel WelcomeRow\n");
        _plan = builderless_state_view_model.Plan(source.Replace("slice StateView WelcomeInfo\n      readmodel WelcomeRow", "slice StateView Rows\n      readmodel WelcomeRow\n        id Uuid key\n        text String\n    slice StateView WelcomeInfo\n      readmodel Other", StringComparison.Ordinal));
    }

    [Fact] void should_still_refuse_the_structural_defect() => _plan.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-007");
    [Fact] void should_fail() => _plan.Success.ShouldBeFalse();
}
