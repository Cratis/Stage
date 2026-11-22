// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_semantic_exclusions;

public class and_rendering_a_strict_service : given.an_excluded_feature
{
    void Establish() => _request = _request with
    {
        Profile = DomainPlacementInput.WithDomain(CratisRendering.CreateProfile(_model.Application.Name, _options with { Strict = true, ServiceLayout = true }), "Sales/Orders")
    };

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_fail_acceptance() => _plan.Success.ShouldBeFalse();
    [Fact] void should_not_block_publication() => _plan.CanPublishArtifacts.ShouldBeTrue();
    [Fact] void should_preserve_service_and_domain_placement() => Comment.RelativePath.ShouldEqual("Source/Projects/Sales/Orders/Projects/Registration/ScreenplayExclusions.g.cs");
    [Fact] void should_place_the_report_at_the_service_root() => Report.RelativePath.ShouldEqual("Source/Projects/UNSUPPORTED.md");
    [Fact] void should_keep_fixed_plan_evidence() => _plan.PlanEvidence.Length.ShouldEqual(1);
}
