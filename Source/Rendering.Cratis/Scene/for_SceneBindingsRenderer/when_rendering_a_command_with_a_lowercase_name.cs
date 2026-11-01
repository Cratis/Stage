// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_SceneBindingsRenderer;

public class when_rendering_a_command_with_a_lowercase_name : a_register_project_render_request
{
    string _bindings = null!;

    void Establish()
    {
        var registration = _registerProject with { Commands = [.. _registerProject.Commands.Select(_ => _ with { Name = "registerProject" })] };
        var feature = _feature with { Slices = [registration, _projectLookup] };
        var application = _model.Application with { Modules = [_module with { Features = [feature] }] };
        var model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application);
        _bindings = SceneBindingsRenderer.Render(new SemanticApplicationContext(new(model, _executionPlan, _request.Profile, _request.Scope), _options));
    }

    [Fact] void should_import_the_generated_pascal_case_export() => _bindings.ShouldContain("import { RegisterProject as __sceneCommand0 } from '../Projects/Registration/RegisterProject';");
    [Fact] void should_register_the_command_by_its_authored_name() => _bindings.ShouldContain("registerCommands({\"registerProject\": __sceneCommand0});");
}
