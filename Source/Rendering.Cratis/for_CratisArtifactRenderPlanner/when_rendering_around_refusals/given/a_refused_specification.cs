// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals.given;

public class a_refused_specification : for_CratisArtifactRenderPlanner.given.a_collecting_render_request
{
    protected SemanticId _refused;
    protected ArtifactRenderRequest _original = null!;

    void Establish()
    {
        _original = WithSource(Source.Replace(
            "      specification RegisteringAProject\n",
            "      specification RegisteringAProject\n        given readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"\n",
            StringComparison.Ordinal));
        _refused = _original.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Specifications)
            .Single(specification => specification.Name == "RegisteringAProject").Id;
        _request = OptIn(_original);
    }

    protected static ArtifactRenderRequest OptIn(ArtifactRenderRequest request)
    {
        var profile = request.Profile;
        return request with
        {
            Profile = ArtifactRenderProfile.Create(
                profile.Target,
                profile.TargetVersion,
                profile.Renderer,
                profile.RendererVersion,
                [.. profile.Inputs.Where(input => input.Name != RefusalModeInput.Name), RefusalModeInput.Create(ArtifactRefusalMode.RenderAround)])
        };
    }
}
