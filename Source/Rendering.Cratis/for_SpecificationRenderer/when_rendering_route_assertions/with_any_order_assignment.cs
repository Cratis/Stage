// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.when_rendering_route_assertions;

public class with_any_order_assignment : Specification
{
    ArtifactRenderPlan _plan = null!;
    void Because() => _plan = routed_specifications.Plan(routed_specifications.Assignment());
    [Fact] void should_admit_wildcard_and_exact_destinations() => _plan.Success.ShouldBeTrue();
    [Fact] void should_reassign_an_earlier_wildcard_match() => routed_specifications.Code(_plan).ShouldContain("Assign(assignments[fact], visited)");
    [Fact] void should_match_the_route_as_well_as_the_destination() => routed_specifications.Code(_plan).ShouldContain("entry.Event.Context.EventStreamId == \"2026-10\"");
    [Fact] void should_not_use_the_greedy_matcher() => routed_specifications.Code(_plan).Contains("FindIndex", StringComparison.Ordinal).ShouldBeFalse();
}
