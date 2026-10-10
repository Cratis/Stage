// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Forms;
using Cratis.Stage.Contracts.Scene;
using SceneContribution = Cratis.Scene.Model.ContributionPoints.Contribution;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Api;

/// <summary>
/// Turns authored Scene command forms and command actions into runtime elements carrying the command metadata the
/// native Stage frontend needs before it may enable submission.
/// </summary>
static class CommandFormRuntimeMetadata
{
    internal const string ComponentName = "Stage:commandForm";
    const string FormsSlot = "forms";
    const string MetadataStatusProperty = "metadataStatus";
    const string MetadataErrorProperty = "metadataError";

    internal static SceneApplication Apply(SceneApplication scene, IReadOnlyDictionary<string, CommandFormRuntimeCommand> commands) => scene with
    {
        Screens = [.. scene.Screens.Select(screen => Apply(screen, commands))]
    };

    internal static CommandFormRuntimeCommand FromSchema(string command, string schema) => new(command, schema, FieldsFrom(schema));

    internal static IReadOnlyList<FormField> FieldsFrom(string schema) => CommandFormSchemas.Fields(schema);

    static SceneScreens.Screen Apply(SceneScreens.Screen screen, IReadOnlyDictionary<string, CommandFormRuntimeCommand> commands)
    {
        var forms = screen.Forms.Select(form => Enrich(form, commands)).ToArray();
        var formsByCommand = forms
            .GroupBy(form => form.ForCommand, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var slotContent = screen.SlotContent.ToDictionary(
            slot => slot.Key,
            slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(element => Enrich(element, commands, formsByCommand))],
            StringComparer.Ordinal);

        if (forms.Length > 0)
        {
            slotContent[FormsSlot] = [.. slotContent.GetValueOrDefault(FormsSlot) ?? [], .. forms.Select(form => FormElement(screen.Name, form, commands.GetValueOrDefault(form.ForCommand)))];
        }

        return screen with
        {
            Forms = [],
            SlotContent = slotContent,
            Contributions = [.. screen.Contributions.Select(contribution => Enrich(contribution, commands, formsByCommand))]
        };
    }

    static Form Enrich(Form form, IReadOnlyDictionary<string, CommandFormRuntimeCommand> commands)
    {
        if (form.Fields.Count > 0 || !commands.TryGetValue(form.ForCommand, out var command))
        {
            return form;
        }

        return form with { Fields = command.Fields };
    }

    static SceneContribution Enrich(
        SceneContribution contribution,
        IReadOnlyDictionary<string, CommandFormRuntimeCommand> commands,
        IReadOnlyDictionary<string, Form> formsByCommand) => contribution with
    {
        Content = Enrich(contribution.Content, commands, formsByCommand)
    };

    static SceneElements.SceneElement Enrich(
        SceneElements.SceneElement element,
        IReadOnlyDictionary<string, CommandFormRuntimeCommand> commands,
        IReadOnlyDictionary<string, Form> formsByCommand)
    {
        if (element is not SceneElements.ExternalComponent component)
        {
            return element;
        }

        var slots = component.Slots.ToDictionary(
            slot => slot.Key,
            slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(child => Enrich(child, commands, formsByCommand))],
            StringComparer.Ordinal);

        if (!IsAction(component, out var commandName))
        {
            return component with { Slots = slots };
        }

        var properties = new Dictionary<string, object?>(component.Properties, StringComparer.Ordinal);
        var form = formsByCommand.GetValueOrDefault(commandName);
        ApplyCommandMetadata(properties, commandName, commands.GetValueOrDefault(commandName), form?.Fields ?? []);

        if (form is not null)
        {
            properties["form"] = form.Name;
            properties["generationMode"] = form.GenerationMode;
            properties["layout"] = form.Layout;
            if (form.PopulateSource is not null)
            {
                properties["populateSource"] = form.PopulateSource;
            }
        }

        return component with { Properties = properties, Slots = slots };
    }

    static SceneElements.ExternalComponent FormElement(string screen, Form form, CommandFormRuntimeCommand? command)
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["command"] = form.ForCommand,
            ["label"] = form.Name,
            ["generationMode"] = form.GenerationMode,
            ["layout"] = form.Layout,
            ["fields"] = form.Fields
        };

        ApplyCommandMetadata(properties, form.ForCommand, command, form.Fields);

        return new SceneElements.ExternalComponent
        {
            Id = $"form-{screen}-{form.Name}",
            Name = form.Name,
            ComponentName = ComponentName,
            Properties = properties,
            Slots = new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
        };
    }

    static void ApplyCommandMetadata(
        Dictionary<string, object?> properties,
        string commandName,
        CommandFormRuntimeCommand? command,
        IReadOnlyList<FormField> fields)
    {
        if (command is null)
        {
            properties[MetadataStatusProperty] = "missing";
            properties[MetadataErrorProperty] = $"Command metadata for '{commandName}' is missing; Stage will not submit an empty payload.";
            properties.Remove(SceneSynthesizer.SchemaProperty);
            properties.Remove("parameterless");
            if (fields.Count == 0)
            {
                properties.Remove("fields");
            }

            return;
        }

        properties[SceneSynthesizer.SchemaProperty] = command.Schema;
        properties["fields"] = fields.Count > 0 ? fields : command.Fields;
        properties["parameterless"] = command.Fields.Count == 0;
        properties[MetadataStatusProperty] = "available";
        properties.Remove(MetadataErrorProperty);
    }

    static bool IsAction(SceneElements.ExternalComponent component, out string commandName)
    {
        commandName = string.Empty;
        if (!string.Equals(component.ComponentName, "core:action", StringComparison.Ordinal) ||
            !component.Properties.TryGetValue("command", out var commandValue) ||
            commandValue is not string command)
        {
            return false;
        }

        commandName = command;

        return true;
    }
}

sealed record CommandFormRuntimeCommand(string Command, string Schema, IReadOnlyList<FormField> Fields);
