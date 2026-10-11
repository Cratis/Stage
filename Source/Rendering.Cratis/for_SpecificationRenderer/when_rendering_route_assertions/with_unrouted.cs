// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.when_rendering_route_assertions;

public class with_unrouted : Specification
{
    ArtifactRenderPlan _plan = null!;
    void Because() => _plan = routed_specifications.Plan(routed_specifications.WithUnroutedExpectation(routed_specifications.Compile()));
    [Fact] void should_assert_the_default_source() => routed_specifications.Code(_plan).ShouldContain("Event.Context.EventSourceType == global::Cratis.Chronicle.Events.EventSourceType.Default");
    [Fact] void should_assert_all_streams() => routed_specifications.Code(_plan).ShouldContain("Event.Context.EventStreamType == global::Cratis.Chronicle.Events.EventStreamType.All");
    [Fact] void should_assert_the_default_identity() => routed_specifications.Code(_plan).ShouldContain("Event.Context.EventStreamId == global::Cratis.Chronicle.Events.EventStreamId.Default");
}
