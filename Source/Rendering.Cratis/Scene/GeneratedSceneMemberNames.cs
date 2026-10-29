// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>
/// Maps semantic command input destinations in Scene to the generated transport contract.
/// Source bindings, read-model fields and semantic command/query identities remain unchanged.
/// </summary>
internal static class GeneratedSceneMemberNames
{
    internal static void Fields(JsonNode? node, IReadOnlyDictionary<string, string> names)
    {
        if (node is JsonArray array)
        {
            foreach (var item in array) Fields(item, names);
        }
        else if (node is JsonObject value)
        {
            // Scene sourceProperty is a command-member alias, not a read-side population path.
            // Population is supplied separately by interaction/form argument source bindings.
            foreach (var key in new[] { "name", "field", "sourceProperty" })
            {
                if (Text(value, key) is not { } path) continue;
                var emitted = Path(path, names);
                if (emitted == path) continue;
                value[key] = emitted;
            }
            foreach (var (_, child) in value) Fields(child, names);
        }
    }

    internal static void Arguments(JsonNode? node, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> commands)
    {
        if (node is JsonArray array)
        {
            foreach (var item in array) Arguments(item, commands);
        }
        else if (node is JsonObject value)
        {
            if (Text(value, "command") is { } command && commands.TryGetValue(command, out var names))
            {
                if (value["arguments"] is JsonArray arguments)
                {
                    foreach (var argument in arguments.OfType<JsonObject>())
                    {
                        if (Text(argument, "name") is { } name) argument["name"] = Path(name, names);
                    }
                }
                else if (value["arguments"] is JsonObject keyed)
                {
                    RenameKeys(keyed, names);
                }

                if (value["subjectInputs"] is JsonObject subjectInputs)
                {
                    RenameKeys(subjectInputs, names);
                }
            }
            foreach (var (_, child) in value) Arguments(child, commands);
        }
    }

    static void RenameKeys(JsonObject keyed, IReadOnlyDictionary<string, string> names)
    {
        foreach (var (name, source) in keyed.ToArray())
        {
            var emitted = Path(name, names);
            if (emitted == name) continue;
            keyed.Remove(name);
            keyed[emitted] = source;
        }
    }

    static string Path(string path, IReadOnlyDictionary<string, string> names)
    {
        var segments = path.Split('.', 2);
        if (!names.TryGetValue(segments[0], out var name)) return path;
        return segments.Length == 1 ? name : $"{name}.{segments[1]}";
    }

    static string? Text(JsonObject node, string name) =>
        node[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
