// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using SceneElements = Cratis.Scene.Model.Elements;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>
/// Plans the Scene document a generated application's Stage runtime renders: the composed
/// <c language="json">scene.json</c> with the command form metadata a live Stage adds to the Scene it serves.
/// </summary>
/// <remarks>
/// <para>
/// A live Stage serves the authored Scene with every command form and command action carrying its command's schema
/// and fields, and with the screen's forms placed in its <c language="json">forms</c> slot. The runtime builds native
/// forms from exactly that, so a generated application has to run the same document or its forms would differ
/// from the live ones. The schema and fields come from <see cref="CommandFormSchemas"/>, which the live Stage uses
/// too; this applies them to the composed document in the same places the live Stage does.
/// </para>
/// <para>
/// Routes are deliberately not written here. A generated application's routes are whatever its generated Arc
/// proxies register, which the runtime reads from <c language="shell">src/stage.ts</c>; a route guessed at planning time
/// could drift from the one the backend actually serves.
/// </para>
/// </remarks>
internal static class StageSceneDocument
{
    /// <summary>
    /// The planned artifact path.
    /// </summary>
    public const string RelativePath = "src/stage-scene.json";

    const string CommandFormComponent = "Stage:commandForm";
    const string FormsSlot = "forms";

    /// <summary>
    /// Renders the runtime document from the composed Scene payload.
    /// </summary>
    /// <param name="sceneJson">The composed <c language="json">scene.json</c> text.</param>
    /// <param name="context">The indexed semantic application the commands come from.</param>
    /// <returns>The canonical runtime document.</returns>
    /// <exception cref="InvalidArtifactRenderContract">A command schema cannot be converted faithfully.</exception>
    public static string Render(string sceneJson, SemanticApplicationContext context)
    {
        var diagnostics = new ConversionDiagnostics();
        var schemas = context.Commands.Values
            .GroupBy(command => command.Name, StringComparer.Ordinal)
            .Select(group =>
            {
                var properties = group.First().Properties;

                // Contracts serves semantic Host names. Only this generated application's transport uses CLR escapes.
                var names = properties.ToDictionary(property => property.Name, context.MemberNames.WireProperty, StringComparer.Ordinal);
                var schema = CommandFormSchemas.For(context.Application, properties.Select(property => property with { Name = names[property.Name] }), diagnostics, group.Key, SourceLocation.Start);
                var fieldSchema = CommandFormSchemas.For(context.Application, properties, new ConversionDiagnostics(), group.Key, SourceLocation.Start);
                return (Name: group.Key, Schema: schema, FieldSchema: fieldSchema, Names: names);
            })
            .ToArray();
        if (diagnostics.HasErrors)
        {
            throw new InvalidArtifactRenderContract(string.Join(Environment.NewLine, diagnostics.All.Select(error => $"{error.Code}: {error.Message}")));
        }
        var commands = schemas.ToDictionary(
            entry => entry.Name,
            entry =>
            {
                var fields = CanonicalSceneJson.ToNode(CommandFormSchemas.Fields(entry.FieldSchema!))!.AsArray();
                GeneratedSceneMemberNames.Fields(fields, entry.Names);
                return new Command(entry.Schema!, fields, entry.Names);
            },
            StringComparer.Ordinal);

        var document = JsonNode.Parse(sceneJson)!.AsObject();
        StripSampleData(document);

        // A toolbar slice button opens a task dialog built from its command's schema, so the runtime document carries the schemas -
        // and, from the canonical document, the structure - only where one is used. An application with no such button is
        // byte-identical to what it was before slice buttons existed.
        if (CanonicalSceneJson.NamesSlices(document))
        {
            document["commandSchemas"] = new JsonObject(commands
                .OrderBy(command => command.Key, StringComparer.Ordinal)
                .Select(command => new KeyValuePair<string, JsonNode?>(command.Key, JsonValue.Create(command.Value.Schema))));
        }

        if (document["screens"] is JsonArray screens)
        {
            foreach (var screen in screens.OfType<JsonObject>())
            {
                Apply(screen, commands);
            }
        }

        GeneratedSceneMemberNames.Arguments(document, commands.ToDictionary(pair => pair.Key, pair => pair.Value.Names, StringComparer.Ordinal));
        return CanonicalSceneJson.Write(document);
    }

    // Sample data exists to preview with at design time; the runtime document a production application ships carries none (spec SD6).
    static void StripSampleData(JsonObject document)
    {
        document.Remove("sampleDataPacks");
        document.Remove("blueprintSampleDataPacks");
        foreach (var profile in (document["uiProfiles"] as JsonArray ?? []).OfType<JsonObject>())
        {
            profile.Remove("sampleData");
        }
    }

    static void Apply(JsonObject screen, IReadOnlyDictionary<string, Command> commands)
    {
        var forms = (screen["forms"] as JsonArray ?? []).OfType<JsonObject>().Select(form => Enrich(form, commands)).ToArray();
        var formsByCommand = forms
            .Where(form => Text(form, "forCommand") is not null)
            .GroupBy(form => Text(form, "forCommand")!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var slotContent = screen["slotContent"] as JsonObject ?? [];
        foreach (var (_, slot) in slotContent)
        {
            EnrichAll(slot, commands, formsByCommand);
        }

        foreach (var contribution in (screen["contributions"] as JsonArray ?? []).OfType<JsonObject>())
        {
            EnrichAll(contribution["content"], commands, formsByCommand);
        }

        if (forms.Length > 0)
        {
            var slot = slotContent[FormsSlot] as JsonArray ?? [];
            foreach (var form in forms)
            {
                slot.Add(FormElement(Text(screen, "name") ?? string.Empty, form, commands));
            }

            slotContent[FormsSlot] = slot;
            screen["slotContent"] = slotContent;
        }

        screen["forms"] = new JsonArray();
    }

    static JsonObject Enrich(JsonObject form, IReadOnlyDictionary<string, Command> commands)
    {
        var enriched = form.DeepClone().AsObject();
        if (Text(enriched, "forCommand") is { } name && commands.TryGetValue(name, out var command))
        {
            if (Count(enriched["fields"]) == 0)
            {
                enriched["fields"] = command.Fields.DeepClone();
            }
            else
            {
                GeneratedSceneMemberNames.Fields(enriched["fields"], command.Names);
            }
            GeneratedSceneMemberNames.Fields(enriched["layout"], command.Names);
        }

        return enriched;
    }

    static void EnrichAll(JsonNode? node, IReadOnlyDictionary<string, Command> commands, IReadOnlyDictionary<string, JsonObject> formsByCommand)
    {
        switch (node)
        {
            case JsonArray elements:
                foreach (var element in elements)
                {
                    EnrichAll(element, commands, formsByCommand);
                }

                break;

            case JsonObject element:
                foreach (var (_, slot) in element["slots"] as JsonObject ?? [])
                {
                    EnrichAll(slot, commands, formsByCommand);
                }

                if (Text(element, "componentName") == "core:action" && element["properties"] is JsonObject properties && Text(properties, "command") is { } name)
                {
                    var form = formsByCommand.GetValueOrDefault(name);
                    ApplyCommandMetadata(properties, name, commands.GetValueOrDefault(name), form?["fields"]);
                    if (form is not null)
                    {
                        properties["form"] = form["name"]?.DeepClone();
                        properties["generationMode"] = form["generationMode"]?.DeepClone();
                        properties["layout"] = form["layout"]?.DeepClone();
                        if (form["populateSource"] is { } populateSource)
                        {
                            properties["populateSource"] = populateSource.DeepClone();
                        }
                    }
                }

                break;
        }
    }

    static JsonObject FormElement(string screen, JsonObject form, IReadOnlyDictionary<string, Command> commands)
    {
        var command = Text(form, "forCommand") ?? string.Empty;
        var element = CanonicalSceneJson.ToNode(new SceneElements.ExternalComponent
        {
            Id = $"form-{screen}-{Text(form, "name")}",
            Name = Text(form, "name") ?? string.Empty,
            ComponentName = CommandFormComponent,
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal),
            Slots = new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
        })!.AsObject();

        var properties = new JsonObject
        {
            ["command"] = command,
            ["label"] = form["name"]?.DeepClone(),
            ["generationMode"] = form["generationMode"]?.DeepClone(),
            ["layout"] = form["layout"]?.DeepClone(),
            ["fields"] = form["fields"]?.DeepClone() ?? new JsonArray()
        };
        ApplyCommandMetadata(properties, command, commands.GetValueOrDefault(command), form["fields"]);
        element["properties"] = properties;
        return element;
    }

    static void ApplyCommandMetadata(JsonObject properties, string name, Command? command, JsonNode? formFields)
    {
        if (command is null)
        {
            properties["metadataStatus"] = "missing";
            properties["metadataError"] = $"Command metadata for '{name}' is missing; Stage will not submit an empty payload.";
            properties.Remove("schema");
            properties.Remove("parameterless");
            if (Count(formFields) == 0)
            {
                properties.Remove("fields");
            }

            return;
        }

        properties["schema"] = command.Schema;
        properties["fields"] = Count(formFields) > 0 ? formFields!.DeepClone() : command.Fields.DeepClone();
        properties["parameterless"] = command.Fields.Count == 0;
        properties["metadataStatus"] = "available";
        properties.Remove("metadataError");
    }

    static int Count(JsonNode? node) => node is JsonArray array ? array.Count : 0;

    static string? Text(JsonObject node, string name) =>
        node[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    sealed record Command(string Schema, JsonArray Fields, IReadOnlyDictionary<string, string> Names);
}
