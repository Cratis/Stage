// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_StageModuleRenderer;

public class when_rendering_a_command_with_a_lowercase_name : a_register_project_render_request
{
    string _stage = null!;

    void Establish()
    {
        var registration = _registerProject with { Commands = [.. _registerProject.Commands.Select(_ => _ with { Name = "registerProject" })] };
        var feature = _feature with { Slices = [registration, _projectLookup] };
        var application = _model.Application with { Modules = [_module with { Features = [feature] }] };
        var model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application);
        var context = new SemanticApplicationContext(new(model, _executionPlan, _request.Profile, _request.Scope), _options);
        _stage = StageModuleRenderer.Render(context, context.AllSlices);
    }

    [Fact] void should_import_the_generated_pascal_case_export() => _stage.ShouldContain("import { RegisterProject as __stageCommand0 } from '../Projects/Registration/RegisterProject';");
    [Fact] void should_keep_the_authored_name_as_the_route_key() => _stage.ShouldContain("\"registerProject\": new __stageCommand0().route,");
}
