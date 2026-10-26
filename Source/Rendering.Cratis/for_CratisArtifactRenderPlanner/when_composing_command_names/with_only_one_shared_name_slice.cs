// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names;

public class with_only_one_shared_name_slice : given.a_two_module_request
{
    string _bindings = null!;
    string _routes = null!;
    string _scene = null!;
    string _schemas = null!;

    void Establish() => DuplicateCommandName();

    void Because()
    {
        var context = new SemanticApplicationContext(_request, _options);
        LocatedSemanticSlice[] slices = [context.AllSlices.Single(slice => slice.Slice.Id == _registerProject.Id)];
        _bindings = SceneBindingsRenderer.Render(context, slices);
        _routes = StageModuleRenderer.Render(context, slices);
        _scene = CanonicalSceneJson.Serialize(DefaultSceneComposition.Create(context, slices)!);
        _schemas = StageSceneDocument.Render("""{"screens":[],"button":{"slice":"Projects.Registration.RegisterProject"}}""", context);
    }

    [Fact] void should_keep_the_application_wide_binding_key() => _bindings.ShouldContain("\"$command$4:8:Projects12:Registration15:RegisterProject15:RegisterProject\": __sceneCommand0");
    [Fact] void should_keep_the_application_wide_route_key() => _routes.ShouldContain("\"$command$4:8:Projects12:Registration15:RegisterProject15:RegisterProject\": new __stageCommand0().route");
    [Fact] void should_keep_the_application_wide_form_key() => _scene.ShouldContain("\"command\":\"$command$4:8:Projects12:Registration15:RegisterProject15:RegisterProject\"");
    [Fact] void should_publish_the_original_schema_under_its_application_wide_key() => _schemas.ShouldContain("\"$command$4:8:Projects12:Registration15:RegisterProject15:RegisterProject\":");
    [Fact] void should_publish_the_additional_schema_under_its_application_wide_key() => _schemas.ShouldContain("\"$command$4:13:OtherProjects12:Registration17:OtherRegistration15:RegisterProject\":");
    [Fact] void should_not_import_the_absent_slice() => _bindings.Contains("OtherRegistration", StringComparison.Ordinal).ShouldBeFalse();
}
