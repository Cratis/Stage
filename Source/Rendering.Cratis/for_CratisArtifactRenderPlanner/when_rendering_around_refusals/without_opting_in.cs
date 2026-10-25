// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class without_opting_in : given.a_refused_specification
{
    void Because() => _plan = _planner.Plan(_original);

    [Fact] void should_report_partial_rendering() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_omit_refused_parts_and_compile_retained_artifacts() => global::Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given.rendering_refusals.OmittedParts(_plan);
    [Fact] void should_not_list_omissions() => _plan.Refusals.ShouldBeEmpty();
    [Fact] void should_include_the_partial_plan_metadata_in_the_digest() => _plan.Digest.ShouldEqual(ArtifactRenderPlan.ComputeDigest(_plan.Target, _plan.TargetVersion, _plan.Renderer, _plan.RendererVersion, _plan.ApplicationName, _plan.Artifacts, _plan.Diagnostics, _plan.ImplementationGaps, _plan.Refusals, _plan.RequiredRemovals));
}
