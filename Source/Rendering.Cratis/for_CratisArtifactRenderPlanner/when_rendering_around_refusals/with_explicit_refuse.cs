// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_explicit_refuse : given.a_refused_specification
{
    ArtifactRenderPlan _default = null!;

    void Establish()
    {
        var profile = _original.Profile;
        _request = _original with
        {
            Profile = ArtifactRenderProfile.Create(
                profile.Target,
                profile.TargetVersion,
                profile.Renderer,
                profile.RendererVersion,
                [.. profile.Inputs, RefusalModeInput.Create(ArtifactRefusalMode.Refuse)])
        };
    }

    void Because()
    {
        _default = _planner.Plan(_original);
        _plan = _planner.Plan(_request);
    }

    [Fact] void should_preserve_the_default_output_digest() => _plan.Digest.ShouldEqual(_default.Digest);
    [Fact] void should_preserve_the_default_diagnostics() => _plan.Diagnostics.SequenceEqual(_default.Diagnostics).ShouldBeTrue();
    [Fact] void should_skip_unrenderable_specifications() => _plan.Success.ShouldBeTrue();
    [Fact] void should_complete_the_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
    [Fact] void should_report_the_single_omitted_specification() => _plan.Refusals.Single().Artifact.ShouldEqual(_refused);
}
