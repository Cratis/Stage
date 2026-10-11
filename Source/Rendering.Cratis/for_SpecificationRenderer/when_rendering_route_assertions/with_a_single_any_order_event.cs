// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.when_rendering_route_assertions;

public class with_a_single_any_order_event : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = routed_specifications.Plan(routed_specifications.SingleAnyOrder());

    [Fact] void should_admit_conflicting_sources_as_assertions() => _plan.Success.ShouldBeTrue();
    [Fact] void should_assert_both_explicit_sources() => routed_specifications.Code(_plan).ShouldContain("entry.Event.Context.EventSourceId == \"acc-2\" && entry.Event.Context.EventSourceId == \"acc-1\"");
    [Fact] void should_assert_the_route() => routed_specifications.Code(_plan).ShouldContain("entry.Event.Context.EventStreamId == \"2026-10\"");
    [Fact] void should_require_one_actual_event() => routed_specifications.Code(_plan).ShouldContain("actual.Length.ShouldEqual(1);");
}
