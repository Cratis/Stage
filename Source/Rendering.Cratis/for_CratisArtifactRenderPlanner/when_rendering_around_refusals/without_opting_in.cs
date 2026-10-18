// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class without_opting_in : given.a_refused_specification
{
    void Because() => _plan = _planner.Plan(_original);

    [Fact] void should_refuse_the_entire_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_list_omissions() => _plan.Refusals.ShouldBeEmpty();
    [Fact] void should_preserve_the_digest_without_refusal_metadata() => _plan.Digest.ShouldEqual(ArtifactRenderPlan.ComputeDigest(_plan.Target, _plan.TargetVersion, _plan.Renderer, _plan.RendererVersion, _plan.ApplicationName, _plan.Artifacts, _plan.Diagnostics));
}
