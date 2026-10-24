// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scene;

internal static class CollectionActionMetadata
{
    public static void Apply(JsonObject screen, SemanticApplicationContext context)
    {
        var elements = Descend(screen).ToArray();
        var carried = elements.Where(element => Text(element, "componentName") == "core:table")
            .SelectMany(table => RowProperties(table["properties"] as JsonObject, context)).ToHashSet(StringComparer.Ordinal);
        foreach (var action in elements.Where(element => Text(element, "componentName") == "core:action")) Bind(action, carried, context);
    }

    static void Bind(JsonObject action, HashSet<string> carried, SemanticApplicationContext context)
    {
        if (action["properties"] is not JsonObject properties || Text(properties, "command") is not { } name) return;
        var identifiers = context.Commands.Values.Where(command => command.Name == name).SelectMany(command => command.Properties)
            .Where(property => property.IsIdentifier && !property.IsGenerated && !property.Type.IsOptional)
            .Select(context.MemberNames.WireProperty).Where(carried.Contains).ToHashSet(StringComparer.Ordinal);
        if (identifiers.Count == 0) return;
        properties["rowAction"] = true;
        if (properties["fields"] is JsonArray fields) properties["fields"] = VisibleFields(fields, identifiers);
    }

    static JsonArray VisibleFields(JsonArray fields, HashSet<string> identifiers) => new([.. fields
        .Where(field => field is not JsonObject member || !identifiers.Contains(Text(member, "sourceProperty") ?? Text(member, "name") ?? string.Empty))
        .Select(field => field?.DeepClone())]);

    static IEnumerable<string> RowProperties(JsonObject? properties, SemanticApplicationContext context)
    {
        var target = properties is null ? null : Text(properties, "typeName");
        return context.ReadModels.Values.Where(model => model.Name == target || target?.EndsWith($".{model.Name}", StringComparison.Ordinal) == true)
            .SelectMany(model => model.Properties.Select(context.MemberNames.WireProperty));
    }

    static IEnumerable<JsonObject> Descend(JsonNode? node) => node switch
    {
        JsonObject element => new[] { element }.Concat(element.SelectMany(property => Descend(property.Value))),
        JsonArray array => array.SelectMany(Descend),
        _ => []
    };

    static string? Text(JsonObject node, string name) => node[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
