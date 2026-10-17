// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_a_scoped_render : given.a_refused_specification
{
    void Establish() => _request = _request with { Scope = new(ArtifactRenderScopeKind.Slice, _registerProject.Id) };
    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_render_a_partial_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_omit_the_refused_source() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_refused);
    [Fact] void should_not_emit_the_application_scaffold() => _plan.Artifacts.ShouldNotContain(artifact => artifact.RelativePath == "Program.cs");
    [Fact] void should_not_render_the_other_slice() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_projectLookup.Id);
}
