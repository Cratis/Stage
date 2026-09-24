// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// The canonical application, planned with a composed Scene carried by the profile.
/// </summary>
/// <remarks>
/// The Scene is supplied rather than derived, which is how a real caller works: it compiles Screenplay once and
/// already holds both the executable semantic model and the translated Scene for that same compile.
/// </remarks>
public class a_composed_register_project_application : a_register_project_render_request
{
    protected SceneApplication _scene = null!;
    protected ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var layout = DefaultLayout.Create();
        _scene = new SceneApplication(
            [],
            [],
            [layout],
            [],
            [],
            [Screen(layout)]);
        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options, _scene);
        _request = new(_model, _executionPlan, profile, new(ArtifactRenderScopeKind.Application, _model.Application.Id));
    }

    protected PlannedArtifact? Artifact(string relativePath) =>
        _plan.Artifacts.FirstOrDefault(_ => string.Equals(_.RelativePath, relativePath, StringComparison.Ordinal));

    static SceneScreens.Screen Screen(SceneLayouts.Layout layout) =>
        new(
            "register-project",
            layout.Name,
            new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal),
            [],
            []);
}
