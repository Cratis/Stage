// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_generation_rules;

public class without_a_pin_for_concept_rules : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = opaque_policy_model.Plan(generation_rule_model.Load(validation: "  validate\n    not empty", pinned: false));

    [Fact] void should_omit_the_unproven_fixture() => rendering_refusals.Rejected(_plan, "STAGE-ESM-038");
    [Fact] void should_require_a_pin_for_unproven_rules() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-038");
}
#endif
