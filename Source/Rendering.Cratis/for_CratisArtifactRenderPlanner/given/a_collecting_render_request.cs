// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_collecting_render_request : a_register_project_render_request
{
    protected ArtifactRenderPlan _plan = null!;

    protected ArtifactRenderRequest WithSource(string source)
    {
        var form = Corpus.SourceForms.Single(candidate => candidate.Name == "single");
        var document = form.Documents.Single();
        var model = Compile(form with { Documents = [document with { Bytes = [.. Encoding.UTF8.GetBytes(source)] }] }).Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Xunit.Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));

        return _request with { Model = model, ExecutionPlan = execution.Plan! };
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
