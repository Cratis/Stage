// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer;

public class when_rendering_conditional_route_assertions : Specification
{
    ArtifactRenderPlan _plan;

    void Because() => _plan = given.routed_specifications.Plan(given.conditional_routed_specifications.Model());

    [Fact] void should_admit_the_guarded_property_route() => _plan.Success.ShouldBeTrue();
    [Fact] void should_assert_the_exact_taken_count() => given.routed_specifications.Code(_plan).ShouldContain("_scenario.AppendedEvents.Count.ShouldEqual(1)");
}
