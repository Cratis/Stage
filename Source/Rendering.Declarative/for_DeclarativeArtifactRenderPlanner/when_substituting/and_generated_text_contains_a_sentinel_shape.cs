// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_substituting;

public class and_generated_text_contains_a_sentinel_shape : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanWith(new() { ["templates/index.sbn"] = "before /*__stage_inline:body*/ after" });

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_residue_refusal() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([DeclarativeCodes.SentinelResidue]);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
}
