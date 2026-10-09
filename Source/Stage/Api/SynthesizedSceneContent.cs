// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Stage.Contracts.Scene;
using SceneElements = Cratis.Scene.Model.Elements;

namespace Cratis.Stage.Api;

internal static class SynthesizedSceneContent
{
    internal static IReadOnlyList<SceneElements.SceneElement> For(
        string sliceName,
        string typeNamespace,
        string? readModelName,
        string? readModelSchema,
        string? commandName,
        CommandFormMetadata? commandMetadata)
    {
        var content = new List<SceneElements.SceneElement>
        {
            SceneElementFactory.Component($"{typeNamespace}.title", "core:title", new Dictionary<string, object?>
            {
                ["text"] = Humanize(sliceName)
            })
        };

        if (readModelName is not null)
        {
            var typeName = $"{typeNamespace}.{ModelNaming.ToIdentifier(readModelName)}";
            content.Add(SceneElementFactory.Component(
                $"{typeNamespace}.data",
                "core:data",
                new Dictionary<string, object?>
                {
                    ["modelName"] = readModelName,
                    ["isCollection"] = true,
                    [SceneSynthesizer.TypeNameProperty] = typeName
                }));

            content.Add(SceneElementFactory.Component(
                $"{typeNamespace}.table",
                "core:table",
                new Dictionary<string, object?>
                {
                    ["target"] = readModelName,
                    [SceneSynthesizer.TypeNameProperty] = typeName
                },
                new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
                {
                    ["columns"] = Columns(typeNamespace, readModelSchema!)
                }));
        }

        if (commandName is not null)
        {
            content.Add(SceneElementFactory.Component(
                $"{typeNamespace}.action",
                "core:action",
                new Dictionary<string, object?>
                {
                    ["command"] = commandName,
                    ["label"] = Humanize(commandName),
                    [SceneSynthesizer.TypeNameProperty] = $"{typeNamespace}.{ModelNaming.ToIdentifier(commandName)}",
                    [SceneSynthesizer.SchemaProperty] = commandMetadata?.Schema,
                    ["fields"] = commandMetadata?.Fields ?? [],
                    ["metadataStatus"] = commandMetadata is null ? "missing" : "available"
                }));
        }

        // A slice with neither a read model nor a command has nothing to show beyond its own name.
        return content.Count > 1 ? content : [];
    }

    static IReadOnlyList<SceneElements.SceneElement> Columns(string typeNamespace, string schema) =>
    [
        .. SchemaProperties(schema).Select(property => (SceneElements.SceneElement)SceneElementFactory.Component(
            $"{typeNamespace}.column.{property}",
            "core:column",
            new Dictionary<string, object?>
            {
                ["property"] = property,
                ["label"] = Humanize(property)
            }))
    ];

    static IReadOnlyList<string> SchemaProperties(string schema)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(schema);
            if (!document.RootElement.TryGetProperty("properties", out var properties) ||
                properties.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            return [.. properties.EnumerateObject().Select(property => property.Name)];
        }
        catch (JsonException)
        {
            // Preserve the existing presentation fallback for a schema that cannot be parsed.
            return [];
        }
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
