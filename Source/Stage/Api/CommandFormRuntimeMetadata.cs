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

    // What the runtime derives from the command and the form for it, which an authored element does not overrule.
    static readonly HashSet<string> _runtimeProperties = new(["command", "fields", "layout", "generationMode", "populateSource", "schema", "parameterless", MetadataStatusProperty, MetadataErrorProperty], StringComparer.Ordinal);

    internal static SceneApplication Apply(SceneApplication scene, IReadOnlyDictionary<string, CommandFormRuntimeCommand> commands) => scene with
    {
        Screens = [.. scene.Screens.Select(screen => Apply(screen, commands))],
        CommandSchemas = !scene.ReferencesSlices ? null : commands.OrderBy(command => command.Key, StringComparer.Ordinal).ToDictionary(command => command.Key, command => command.Value.Schema, StringComparer.Ordinal)
    };

    internal static SceneElements.ExternalComponent PlacedFormElement(SceneElements.ExternalComponent placed, Form form, CommandFormRuntimeCommand? command)
    {
        var element = FormElement(placed.Id, placed.Name, form, command);
        var properties = new Dictionary<string, object?>(element.Properties, StringComparer.Ordinal);
        foreach (var (name, value) in placed.Properties.Where(property => !_runtimeProperties.Contains(property.Key)))
        {
            properties[name] = value;
        }

        return element with { Properties = properties, Behaviors = placed.Behaviors };
    }

    internal static CommandFormRuntimeCommand FromSchema(string command, string schema) => new(command, schema, FieldsFrom(schema));

    internal static IReadOnlyList<FormField> FieldsFrom(string schema) => CommandFormSchemas.Fields(schema);

    static SceneScreens.Screen Apply(SceneScreens.Screen screen, IReadOnlyDictionary<string, CommandFormRuntimeCommand> commands)
    {
        var forms = screen.Forms.Select(form => Enrich(form, commands)).ToArray();
        var formsByCommand = forms
            .GroupBy(form => form.ForCommand, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var placed = new PlacedForms(formsByCommand);
        var slotContent = screen.SlotContent.ToDictionary(
            slot => slot.Key,
            slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(element => Enrich(element, commands, formsByCommand, placed))],
            StringComparer.Ordinal);
        var contributions = screen.Contributions.Select(contribution => Enrich(contribution, commands, formsByCommand, placed)).ToArray();

        // A form a command form element placed on the screen already presents is shown there, beside the elements it was authored
        // among, and is not appended a second time at the end of the screen.
        var unplaced = forms.Where(form => !placed.Commands.Contains(form.ForCommand)).ToArray();
        if (unplaced.Length > 0)
        {
            slotContent[FormsSlot] = [.. slotContent.GetValueOrDefault(FormsSlot) ?? [], .. unplaced.Select(form => FormElement(screen.Name, form, commands.GetValueOrDefault(form.ForCommand)))];
        }

        return screen with
        {
            Forms = [],
            SlotContent = slotContent,
            Contributions = contributions
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
        IReadOnlyDictionary<string, Form> formsByCommand,
        PlacedForms placed) => contribution with
        {
            Content = Enrich(contribution.Content, commands, formsByCommand, placed)
        };

    static SceneElements.SceneElement Enrich(
        SceneElements.SceneElement element,
        IReadOnlyDictionary<string, CommandFormRuntimeCommand> commands,
        IReadOnlyDictionary<string, Form> formsByCommand,
        PlacedForms placed)
    {
        if (element is not SceneElements.ExternalComponent component)
        {
            return element;
        }

        if (IsPlacedCommandForm(component, out var placedCommand))
        {
            return placed.Show(component, placedCommand, commands.GetValueOrDefault(placedCommand));
        }

        var slots = component.Slots.ToDictionary(
            slot => slot.Key,
            slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(child => Enrich(child, commands, formsByCommand, placed))],
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
            properties["onSubmit"] = form.OnSubmit;
            properties["generationMode"] = form.GenerationMode;
            properties["layout"] = form.Layout;
            if (form.PopulateSource is not null)
            {
                properties["populateSource"] = form.PopulateSource;
            }
        }

        return component with { Properties = properties, Slots = slots };
    }

    static SceneElements.ExternalComponent FormElement(string screen, Form form, CommandFormRuntimeCommand? command) =>
        FormElement($"form-{screen}-{form.Name}", form.Name, form, command);

    static SceneElements.ExternalComponent FormElement(string id, string name, Form form, CommandFormRuntimeCommand? command)
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["command"] = form.ForCommand,
            ["label"] = form.Name,
            ["generationMode"] = form.GenerationMode,
            ["layout"] = form.Layout,
            ["fields"] = form.Fields,
            ["onSubmit"] = form.OnSubmit
        };

        if (form.PopulateSource is not null)
        {
            properties["populateSource"] = form.PopulateSource;
        }

        ApplyCommandMetadata(properties, form.ForCommand, command, form.Fields);

        return new SceneElements.ExternalComponent
        {
            Id = id,
            Name = name,
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

    // The names a command form placed on a screen goes by: the Cratis components package's, as Studio stores it and as a Screenplay
    // component names it, and the runtime's own.
    static bool IsPlacedCommandForm(SceneElements.ExternalComponent component, out string commandName)
    {
        commandName = string.Empty;
        if (component.ComponentName is not ("Cratis.Components:commandForm" or "Cratis.Components.commandForm" or ComponentName) ||
            !component.Properties.TryGetValue("command", out var commandValue) ||
            commandValue is not string { Length: > 0 } command)
        {
            return false;
        }

        commandName = command;

        return true;
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

/// <summary>
/// The command forms the elements placed on a screen present. A placed command form takes the form its screen has for its
/// command - the form's fields, geometry and generation - or the form that follows the command's own schema, and keeps its
/// place, its identity and every property it was authored with.
/// </summary>
/// <param name="formsByCommand">The forms the screen has, by the command each is for.</param>
sealed class PlacedForms(IReadOnlyDictionary<string, Form> formsByCommand)
{
    readonly HashSet<string> _commands = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the commands whose forms an element placed on the screen presents.
    /// </summary>
    public IReadOnlySet<string> Commands => _commands;

    /// <summary>
    /// Makes a placed command form element the runtime command form for its command.
    /// </summary>
    /// <param name="component">The placed element.</param>
    /// <param name="commandName">The command it presents.</param>
    /// <param name="command">What the model knows of the command, when it knows it.</param>
    /// <returns>The runtime command form, where the element was placed.</returns>
    internal SceneElements.ExternalComponent Show(SceneElements.ExternalComponent component, string commandName, CommandFormRuntimeCommand? command)
    {
        _commands.Add(commandName);
        var form = formsByCommand.GetValueOrDefault(commandName) ?? new Form(commandName, commandName, null, [], FormGenerationMode.Auto);
        return CommandFormRuntimeMetadata.PlacedFormElement(component, form, command);
    }
}
