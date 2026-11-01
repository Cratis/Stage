// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.given;

public class an_omission_context : a_dependency_index
{
    internal SemanticApplicationContext _context = null!;

    protected static ImmutableDictionary<SemanticId, string> Roots(SemanticId root) =>
        ImmutableDictionary<SemanticId, string>.Empty.Add(root, "STAGE-DEGRADE-001");

    protected void BuildContext(ArtifactRenderProfile? profile = null)
    {
        var application = _application with
        {
            Modules = [_module with { Features = [_feature with { Slices = [_registerProject, _projectLookup] }] }]
        };
        var model = _model with { };
        Replace(model, "Application", application);
        _context = new SemanticApplicationContext(_request with { Model = model, Profile = profile ?? _request.Profile }, _options);
        _index = new(_context);
    }

    protected ArtifactRenderProfile WithAuthoredScene()
    {
        var profile = _request.Profile;
        var scene = CratisArtifactRenderInput.CreateText("scene.json", CratisRendering.TargetVersion, "{\"toolbar\":{},\"screens\":[]}");
        return ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, scene]);
    }
}
