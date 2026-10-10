// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.when_rendering_route_assertions;

public class with_a_then_route : Specification
{
    ArtifactRenderPlan _plan = null!;
    void Because() => _plan = routed_specifications.Plan(routed_specifications.Compile());
    [Fact] void should_admit_routed_specifications() => _plan.Success.ShouldBeTrue();
    [Fact] void should_assert_the_stored_source() => routed_specifications.Code(_plan).ShouldContain("Event.Context.EventSourceType == \"stored-account\"");
    [Fact] void should_assert_the_stored_stream() => routed_specifications.Code(_plan).ShouldContain("Event.Context.EventStreamType == \"stored-transactions\"");
    [Fact] void should_assert_the_stream_identity() => routed_specifications.Code(_plan).ShouldContain("Event.Context.EventStreamId == \"2026-10\"");
    [Fact] void should_not_register_event_sources_by_hand() => routed_specifications.Code(_plan).Contains("IEventSources", StringComparison.Ordinal).ShouldBeFalse();
}
