// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Realization;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ModelAssetsRendering;

public class when_admitting_invalid_assets : for_CratisArtifactRenderPlanner.given.a_register_project_render_request
{
    [Theory]
    [InlineData("not json")]
    [InlineData("{\"../secret.json\":\"AQ==\"}")]
    [InlineData("{\"image.png\":\"invalid\"}")]
    [InlineData("{\"Logo.svg\":\"AQ==\",\"logo.svg\":\"Ag==\"}")]
    [InlineData("{\"index-abc123.js\":\"AQ==\"}")]
    void should_reject_malformed_assets(string json)
    {
        var input = ArtifactRenderInput.Create(ModelAssetsInput.Name, ModelAssetsInput.Version, [.. Encoding.UTF8.GetBytes(json)]);
        Plan([input]).Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-CRATIS-001").ShouldBeTrue();
    }

    [Fact]
    void should_reject_duplicate_assets()
    {
        var input = ArtifactRenderInput.Create(ModelAssetsInput.Name, ModelAssetsInput.Version, [.. Encoding.UTF8.GetBytes("{}")]);
        Assert.Throws<InvalidArtifactRenderContract>(() => Plan([input, input]));
    }

    ArtifactRenderPlan Plan(ImmutableArray<ArtifactRenderInput> inputs)
    {
        var profile = _request.Profile;
        return new CratisArtifactRenderPlanner().Plan(_request with
        {
            Profile = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, .. inputs])
        });
    }
}
