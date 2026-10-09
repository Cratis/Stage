// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Scene;
using SceneContribution = Cratis.Scene.Model.ContributionPoints.Contribution;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneForms = Cratis.Scene.Model.Forms;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Api;

internal static class CommandFormMetadataEnricher
{
    const string MetadataStatusProperty = "metadataStatus";
    const string MetadataErrorProperty = "metadataError";

    internal static SceneApplication Enrich(
        SceneApplication scene,
        IReadOnlyDictionary<string, CommandFormMetadata> commands) => scene with
        {
            Screens = [.. scene.Screens.Select(screen => Enrich(screen, commands))]
        };

    static SceneScreens.Screen Enrich(
        SceneScreens.Screen screen,
        IReadOnlyDictionary<string, CommandFormMetadata> commands)
    {
        var forms = screen.Forms.Select(form => Enrich(form, commands)).ToList();
        var formsByCommand = forms
            .GroupBy(form => form.ForCommand, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        return screen with
        {
            Forms = forms,
            SlotContent = screen.SlotContent.ToDictionary(
                slot => slot.Key,
                slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(element => Enrich(element, commands, formsByCommand))],
                StringComparer.Ordinal),
            Contributions = [.. screen.Contributions.Select(contribution => Enrich(contribution, commands, formsByCommand))]
        };
    }

    static SceneForms.Form Enrich(
        SceneForms.Form form,
        IReadOnlyDictionary<string, CommandFormMetadata> commands)
    {
        if (form.Fields.Count > 0 || !commands.TryGetValue(form.ForCommand, out var command))
        {
            return form;
        }

        return form with
        {
            Fields = [.. command.Fields.Select(field => new SceneForms.FormField(field.Name, field.SourceProperty, field.ComposeUsing, field.Label, field.Placement))]
        };
    }

    static SceneContribution Enrich(
        SceneContribution contribution,
        IReadOnlyDictionary<string, CommandFormMetadata> commands,
        IReadOnlyDictionary<string, SceneForms.Form> formsByCommand) => contribution with
    {
        Content = Enrich(contribution.Content, commands, formsByCommand)
    };

    static SceneElements.SceneElement Enrich(
        SceneElements.SceneElement element,
        IReadOnlyDictionary<string, CommandFormMetadata> commands,
        IReadOnlyDictionary<string, SceneForms.Form> formsByCommand)
    {
        if (element is not SceneElements.ExternalComponent component)
        {
            return element;
        }

        var slots = component.Slots.ToDictionary(
            slot => slot.Key,
            slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(child => Enrich(child, commands, formsByCommand))],
            StringComparer.Ordinal);

        if (!string.Equals(component.ComponentName, "core:action", StringComparison.Ordinal) ||
            !component.Properties.TryGetValue("command", out var commandValue) ||
            commandValue is not string commandName)
        {
            return component with { Slots = slots };
        }

        var properties = new Dictionary<string, object?>(component.Properties, StringComparer.Ordinal);
        if (!commands.TryGetValue(commandName, out var command))
        {
            properties[MetadataStatusProperty] = "missing";
            properties[MetadataErrorProperty] = $"Command metadata for '{commandName}' is missing; Stage will not submit an empty payload.";

            return component with { Properties = properties, Slots = slots };
        }

        var form = formsByCommand.GetValueOrDefault(commandName);
        var fields = form is { Fields.Count: > 0 }
            ? [.. form.Fields.Select(CommandFormField.From)]
            : command.Fields;

        properties[SceneSynthesizer.SchemaProperty] = command.Schema;
        properties["fields"] = fields;
        properties[MetadataStatusProperty] = "available";

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
}
