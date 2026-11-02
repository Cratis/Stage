// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Lifecycles;

/// <summary>
/// Describes the subject of a lifecycle in JSON and fills the commands it sends.
/// </summary>
/// <remarks>
/// A command a lifecycle sends receives the subject key as its identifier and, for every other property, the member of the
/// subject (or of the item a fan-out branch works on) that has the same name. How the executor fills the rest is its contract.
/// </remarks>
internal static class LifecycleSubjects
{
    internal static string KeyText(SemanticValue key) => Convert.ToString(SemanticJsonValues.ToObject(key), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

    internal static JsonObject SubjectJson(SemanticExecutionPlan plan, SemanticId readModel, SemanticValue key, ImmutableArray<SemanticPropertyValue> values)
    {
        var subject = new JsonObject { ["key"] = KeyText(key) };
        if (!plan.ReadModels.TryGetValue(readModel, out var model)) return subject;
        foreach (var value in values)
        {
            if (model.Properties.FirstOrDefault(property => property.Id == value.TargetProperty) is not { } property) continue;
            subject[CamelCase(property.Name)] = Keyed(JsonSerializer.SerializeToNode(SemanticJsonValues.ToObject(value.Value, property.Type, plan.Model.Application)));
        }

        return subject;
    }

    // The workflow engine fans out over a list of objects that each have a unique string 'key'. Screenplay lists carry no such key, so
    // the position is the key, and a scalar element becomes an object holding its 'value'.
    static JsonNode? Keyed(JsonNode? node)
    {
        if (node is not JsonArray array) return node;
        var keyed = new JsonArray();
        for (var index = 0; index < array.Count; index++)
        {
            var item = array[index];
            var entry = item is JsonObject existing ? JsonNode.Parse(existing.ToJsonString())!.AsObject() : new JsonObject { ["value"] = item?.DeepClone() };
            if (!entry.ContainsKey("key")) entry.Insert(0, "key", index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            keyed.Add(entry);
        }

        return keyed;
    }

    internal static IReadOnlyDictionary<string, JsonElement> CommandPayload(SemanticExecutionPlan plan, SemanticCommand command, string key, params JsonNode?[] sources)
    {
        var payload = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in command.Properties.Where(property => !property.IsGenerated))
        {
            if (property.IsIdentifier)
            {
                payload[property.Name] = JsonSerializer.SerializeToElement(key);
                continue;
            }

            foreach (var source in sources.OfType<JsonObject>())
            {
                var member = source.FirstOrDefault(candidate => string.Equals(candidate.Key, property.Name, StringComparison.OrdinalIgnoreCase));
                if (member.Value is null) continue;
                payload[property.Name] = member.Value.Deserialize<JsonElement>();
                break;
            }
        }

        _ = plan;
        return payload;
    }

    internal static string CamelCase(string name) => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
