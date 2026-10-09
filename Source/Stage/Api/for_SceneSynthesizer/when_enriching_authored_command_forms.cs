// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

using SceneElements = Cratis.Scene.Model.Elements;
using SceneForms = Cratis.Scene.Model.Forms;

namespace Cratis.Stage.Api.for_SceneSynthesizer;

public class when_enriching_authored_command_forms : given.a_scene_model
{
    SceneApplication _result = null!;
    SceneElements.ExternalComponent _form = null!;

    void Establish()
    {
        var authored = _scene with
        {
            Screens =
            [
                new(
                    "ProjectRegistration",
                    _scene.Layouts[0].Name,
                    new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal),
                    [new SceneForms.Form("Register project", "RegisterProject", null, [], SceneForms.FormGenerationMode.Auto)],
                    [])
            ]
        };

        _result = SceneSynthesizer.Synthesize(authored, _semanticModel);
        _form = _result.Screens.Single().SlotContent["forms"].OfType<SceneElements.ExternalComponent>().Single();
    }

    [Fact] void should_turn_the_scene_form_into_a_runtime_command_form() => _form.ComponentName.ShouldEqual("Stage:commandForm");
    [Fact] void should_clear_the_scene_form_collection_so_the_frontend_does_not_render_a_duplicate() => _result.Screens.Single().Forms.ShouldBeEmpty();
    [Fact] void should_carry_the_command_name() => _form.Properties["command"].ShouldEqual("RegisterProject");
    [Fact] void should_carry_the_schema() => ((string)_form.Properties["schema"]!).ShouldContain("\"projectId\"");
    [Fact] void should_carry_required_metadata() => ((string)_form.Properties["schema"]!).ShouldContain("\"required\":[\"projectId\",\"name\",\"stage\",\"tags\"]");
    [Fact] void should_generate_fields_from_the_command_schema() => Fields.Select(_ => _.Name).ShouldContainOnly(["projectId", "name", "stage", "description", "tags"]);
    [Fact] void should_not_mark_a_command_with_inputs_as_parameterless() => _form.Properties["parameterless"].ShouldEqual(false);

    IReadOnlyList<SceneForms.FormField> Fields => (IReadOnlyList<SceneForms.FormField>)_form.Properties["fields"]!;
}
