// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_collecting_render_request : a_register_project_render_request
{
    protected ArtifactRenderPlan _plan = null!;

    protected ArtifactRenderRequest WithChange(SemanticSlice changed)
    {
        var application = _model.Application with
        {
            Modules = [_module with { Features = [_feature with { Slices = [.. _feature.Slices.Select(slice => slice.Id == changed.Id ? changed : slice)] }] }]
        };
        return _request with { Model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application) };
    }

    protected ArtifactRenderRequest WithGuard(ArtifactRenderRequest request)
    {
        var input = CratisArtifactRenderInput.CreateText("scene.json", CratisRendering.TargetVersion,
            """{"uiProfiles":[],"themes":[],"layouts":[],"screenTemplates":[],"dialogTemplates":[],"screens":[{"name":"Projects","slotContent":{"content":[{"componentName":"core:action","properties":{"otherwise":[]}}]}}]}""");
        var profile = request.Profile;
        return request with
        {
            Profile = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, input])
        };
    }
}
