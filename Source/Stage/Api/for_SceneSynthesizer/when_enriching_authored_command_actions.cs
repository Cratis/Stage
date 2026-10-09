// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Forms;
using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Api.for_SceneSynthesizer;

public class when_enriching_authored_command_actions : given.a_scene_model
{
    SceneApplication _result = null!;
    string _payload = null!;

    void Because()
    {
        var action = new ExternalComponent
        {
            Id = "register-project",
            Name = "Register project",
            ComponentName = "core:action",
            Properties = new Dictionary<string, object?> { ["command"] = "RegisterProject", ["label"] = "Register project" },
            Slots = new Dictionary<string, IReadOnlyList<SceneElement>>()
        };
        var screen = new Screen(
            "ProjectRegistration",
            _scene.Layouts[0].Name,
            new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal) { [DefaultLayout.ContentSlotName] = [action] },
            [new Form("RegisterProjectForm", "RegisterProject", null, [], null, null)],
            []);

        _result = SceneSynthesizer.Synthesize(_scene with { Screens = [screen] }, _semanticModel);
        _payload = JsonSerializer.Serialize(_result, StageJson.Options);
    }

    [Fact] void should_expose_the_command_schema() => ActionProperties[SceneSynthesizer.SchemaProperty].ShouldNotBeNull();
    [Fact] void should_mark_the_metadata_as_available() => ActionProperties["metadataStatus"].ShouldEqual("available");
    [Fact] void should_generate_fields_from_the_command_schema() => Fields.Select(field => field.Name).ShouldContainOnly(["projectId", "name", "stage", "description", "tags"]);
    [Fact] void should_generate_human_readable_field_labels() => Fields.Select(field => field.Label).ShouldContainOnly(["Project Id", "Name", "Stage", "Description", "Tags"]);
    [Fact] void should_keep_required_schema_metadata() => Required.ShouldContainOnly(["projectId", "name", "stage", "tags"]);
    [Fact] void should_serialize_the_field_name_into_the_runtime_payload() => _payload.ShouldContain("\"name\":\"projectId\"");
    [Fact] void should_serialize_the_field_label_into_the_runtime_payload() => _payload.ShouldContain("\"label\":\"Project Id\"");

    IReadOnlyDictionary<string, object?> ActionProperties => _result.Screens.Single().SlotContent[DefaultLayout.ContentSlotName].OfType<ExternalComponent>().Single().Properties;
    IReadOnlyList<FormField> Fields => (IReadOnlyList<FormField>)ActionProperties["fields"]!;
    IReadOnlyList<string> Required
    {
        get
        {
            using var document = JsonDocument.Parse((string)ActionProperties[SceneSynthesizer.SchemaProperty]!);

            return [.. document.RootElement.GetProperty("required").EnumerateArray().Select(item => item.GetString()!)];
        }
    }
}
