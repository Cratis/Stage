// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>Omits only the actions and bindings the runtime cannot safely evaluate.</summary>
static class SceneGuardGaps
{
    internal static string WithoutUnevaluableGuards(string json)
    {
        var document = JsonNode.Parse(json)!;
        RemoveUnevaluableGuards(document);
        return CanonicalSceneJson.Write(document);
    }

    static bool IsUnevaluable(JsonNode? node) => node is JsonObject obj &&
        ((SceneGuards.IsGuardedAction(obj) && !SceneGuards.CanRunAction(obj["properties"] as JsonObject)) ||
         (obj.ContainsKey("trigger") && !SceneGuards.CanRunBinding(obj["condition"])));

    static void RemoveUnevaluableGuards(JsonNode node)
    {
        if (node is JsonArray array) RemoveFromArray(array);
        else if (node is JsonObject obj) RemoveFromObject(obj);
    }

    static void RemoveFromArray(JsonArray array)
    {
        foreach (var child in array.ToArray())
        {
            if (IsUnevaluable(child)) array.Remove(child);
            else if (child is not null) RemoveUnevaluableGuards(child);
        }
    }

    static void RemoveFromObject(JsonObject obj)
    {
        foreach (var (key, child) in obj.ToArray())
        {
            if (IsUnevaluable(child)) obj.Remove(key);
            else if (child is not null) RemoveUnevaluableGuards(child);
        }
    }
}
