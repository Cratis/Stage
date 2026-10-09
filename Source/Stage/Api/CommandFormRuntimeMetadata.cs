// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Forms;
using Cratis.Stage.Contracts.Scene;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Api;

/// <summary>
/// Turns authored Scene command forms into runtime elements carrying the command metadata the native Stage
/// frontend needs before it may enable submission.
/// </summary>
static class CommandFormRuntimeMetadata
{
    internal const string ComponentName = "Stage:commandForm";
    const string FormsSlot = "forms";

    internal static SceneApplication Apply(SceneApplication scene, IReadOnlyDictionary<string, CommandFormRuntimeCommand> commands) =>
        scene.Screens.Any(screen => screen.Forms.Count > 0)
            ? scene with { Screens = [.. scene.Screens.Select(screen => Apply(screen, commands))] }
            : scene;

    internal static CommandFormRuntimeCommand FromSchema(string command, string schema) => new(command, schema, FieldsFrom(schema));

    static SceneScreens.Screen Apply(SceneScreens.Screen screen, IReadOnlyDictionary<string, CommandFormRuntimeCommand> commands)
    {
        if (screen.Forms.Count == 0)
        {
            return screen;
        }

        var forms = screen.Forms.Select(form => FormElement(screen.Name, form, commands.GetValueOrDefault(form.ForCommand))).ToArray();
        var slotContent = screen.SlotContent.ToDictionary(_ => _.Key, _ => _.Value, StringComparer.Ordinal);
        slotContent[FormsSlot] = [.. slotContent.GetValueOrDefault(FormsSlot) ?? [], .. forms];

        return screen with
        {
            Forms = [],
            SlotContent = slotContent
        };
    }

    static SceneElements.ExternalComponent FormElement(string screen, Form form, CommandFormRuntimeCommand? command)
    {
        var fields = form.Fields.Count > 0 ? form.Fields : command?.Fields ?? [];
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["command"] = form.ForCommand,
            ["label"] = form.Name,
            ["generationMode"] = form.GenerationMode,
            ["layout"] = form.Layout,
            ["fields"] = fields
        };

        if (command is not null)
        {
            properties["schema"] = command.Schema;
            properties["parameterless"] = command.Fields.Count == 0;
        }

        return new SceneElements.ExternalComponent
        {
            Id = $"form-{screen}-{form.Name}",
            Name = form.Name,
            ComponentName = ComponentName,
            Properties = properties,
            Slots = new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
        };
    }

    static IReadOnlyList<FormField> FieldsFrom(string schema)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return [];
        }

        using var document = JsonDocument.Parse(schema);
        if (!document.RootElement.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return [.. properties.EnumerateObject().Select(property => new FormField(property.Name, null, null, Humanize(property.Name), null))];
    }

    static string Humanize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var text = new System.Text.StringBuilder(value.Length * 2);
        text.Append(char.ToUpperInvariant(value[0]));
        for (var index = 1; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && !char.IsUpper(value[index - 1]))
            {
                text.Append(' ');
            }

            text.Append(character);
        }

        return text.ToString();
    }
}

sealed record CommandFormRuntimeCommand(string Command, string Schema, IReadOnlyList<FormField> Fields);
