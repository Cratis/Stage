// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneForms = Cratis.Scene.Model.Forms;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_CanonicalSceneJson;

public class when_serializing_rich_command_forms : Specification
{
    string _json = null!;

    void Because()
    {
        var layout = DefaultLayout.Create();
        var formLayout = new SceneForms.CommandFormLayout(
            [new SceneForms.FormColumn(1, new SceneForms.FormWidth(SceneForms.FormWidthUnit.Fraction, 1), new SceneForms.FormWidth(SceneForms.FormWidthUnit.Pixels, 240), new SceneForms.FormWidth(SceneForms.FormWidthUnit.Percent, 50))],
            [new SceneForms.FormFieldPlacement("title", 1, 1, null, 1, new SceneForms.FormWidth(SceneForms.FormWidthUnit.Percent, 100))],
            new SceneForms.FormWidth(SceneForms.FormWidthUnit.Pixels, 24),
            new SceneForms.FormWidth(SceneForms.FormWidthUnit.Pixels, 16));
        var form = new SceneForms.Form(
            "CreateWorkItemForm",
            "CreateWorkItem",
            null,
            [new SceneForms.FormField("title", null, null, "Title", null)],
            SceneForms.FormGenerationMode.Manual,
            formLayout);
        var element = new SceneElements.ExternalComponent
        {
            Id = "work-items:list",
            Name = "workItems",
            ComponentName = "scene.web.DataGrid",
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["initialSelection"] = new BindingExpression(string.Empty, BindingSourceKind.Literal, null, null, null, null, null, null, null)
            }
        };
        var screen = new SceneScreens.Screen(
            "WorkItemList",
            layout.Name,
            new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
            {
                [DefaultLayout.ContentSlotName] = [element]
            },
            [form],
            []);

        _json = CanonicalSceneJson.Serialize(new SceneApplication([], [], [layout], [], [], [screen]));
    }

    [Fact] void should_emit_the_manual_generation_mode() => _json.ShouldContain("\"generationMode\":\"manual\"");
    [Fact] void should_emit_layout_columns() => _json.ShouldContain("\"columns\":[{\"index\":1");
    [Fact] void should_emit_fractional_column_width() => _json.ShouldContain("\"width\":{\"unit\":\"fraction\",\"value\":1}");
    [Fact] void should_emit_field_placements() => _json.ShouldContain("\"placements\":[{\"column\":1,\"columnSpan\":1,\"field\":\"title\"");
    [Fact] void should_emit_layout_gaps() => _json.ShouldContain("\"columnGap\":{\"unit\":\"pixels\",\"value\":24}");
    [Fact] void should_emit_exact_component_ids() => _json.ShouldContain("\"id\":\"work-items:list\"");
    [Fact] void should_emit_literal_bindings() => _json.ShouldContain("\"initialSelection\":{\"kind\":\"literal\",\"path\":\"\",\"value\":null}");
}
