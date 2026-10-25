// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer;

public class when_refusing_wrong_conditional_sources : Specification
{
    ArtifactRenderPlan _plan;

    void Because() => _plan = given.routed_specifications.Plan(given.conditional_routed_specifications.Model(wrongSource: true));

    [Fact] void should_refuse_an_unasserted_source_expectation() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-011");
    [Fact] void should_not_publish_the_misleading_specification() => rendering_refusals.Rejected(_plan, "STAGE-ESM-011");
}
