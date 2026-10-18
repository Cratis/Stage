// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_an_invalid_refusal_policy : given.a_refused_specification
{
    [Theory]
    [InlineData("1", "renderaround")]
    [InlineData("2", "RenderAround")]
    public void should_refuse_the_entire_plan(string version, string value)
    {
        var profile = _original.Profile;
        var request = _original with
        {
            Profile = ArtifactRenderProfile.Create(
                profile.Target,
                profile.TargetVersion,
                profile.Renderer,
                profile.RendererVersion,
                [.. profile.Inputs, ArtifactRenderInput.Create(RefusalModeInput.Name, version, [.. Encoding.UTF8.GetBytes(value)])])
        };
        var plan = _planner.Plan(request);
        plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
        plan.Artifacts.ShouldBeEmpty();
        plan.Refusals.ShouldBeEmpty();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-CRATIS-001");
    }
}
