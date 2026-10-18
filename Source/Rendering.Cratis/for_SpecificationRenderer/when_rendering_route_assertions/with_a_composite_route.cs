// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.when_rendering_route_assertions;

public class with_a_composite_route : Specification
{
    ArtifactRenderPlan _plan = null!;
    void Because() => _plan = routed_specifications.Plan(routed_specifications.Composite());
    [Fact] void should_share_the_commands_identity_codec() => routed_specifications.Code(_plan).ShouldContain("StreamIds.Composite(global::Routed.GeneratedEventSources.StreamIds.Uuid(_uuid), \"10\", \"a|b%\")");
    [Fact] void should_emit_the_codec_for_literal_fixture_parts() => _plan.Artifacts.Any(artifact => artifact.RelativePath == "GeneratedEventSources/StreamIds.cs").ShouldBeTrue();
}
