// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.when_rendering_route_assertions;

public class with_an_unkeyed_route : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = routed_specifications.Source
            .Replace("    streamId String\n", string.Empty, StringComparison.Ordinal)
            .Replace("          streamId = period\n", string.Empty, StringComparison.Ordinal)
            .Replace("            streamId = \"previous\"\n", string.Empty, StringComparison.Ordinal)
            .Replace("            streamId = \"2026-10\"", string.Empty, StringComparison.Ordinal);
        _plan = routed_specifications.Plan(routed_specifications.Compile(source));
    }

    [Fact] void should_assert_the_default_identity_without_losing_the_route() => routed_specifications.Code(_plan).ShouldContain("Event.Context.EventStreamId == global::Cratis.Chronicle.Events.EventStreamId.Default");
    [Fact] void should_keep_the_stored_source() => routed_specifications.Code(_plan).ShouldContain("Event.Context.EventSourceType == \"stored-account\"");
    [Fact] void should_not_emit_an_unused_identity_codec() => _plan.Artifacts.Any(artifact => artifact.RelativePath == "GeneratedEventSources/StreamIds.cs").ShouldBeFalse();
}
