// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections;
using System.Reflection;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// Read every typed contract member, including nested mappings, literal values and future record members; never mutate the graph.
internal static class round_trip_semantic_nodes
{
    public static IEnumerable<(object Value, string Path)> Walk(SemanticApplication application)
    {
        var pending = new Stack<(object Value, string Path)>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        pending.Push((application, "/application"));
        while (pending.TryPop(out var node))
        {
            if (!seen.Add(node.Value)) continue;
            yield return node;
            foreach (var child in Children(node.Value, node.Path)) pending.Push(child);
        }
    }

    static IEnumerable<(object Value, string Path)> Children(object value, string path)
    {
        if (value is string) return [];
        if (value is IEnumerable sequence) return Items(sequence, path);
        return HasSemanticMembers(value.GetType()) ? Members(value, path) : [];
    }

    static IEnumerable<(object Value, string Path)> Members(object value, string path)
    {
        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length == 0 && property.GetValue(value) is { } child)
            {
                yield return (child, $"{path}/{property.Name}");
            }
        }
    }

    static IEnumerable<(object Value, string Path)> Items(IEnumerable sequence, string path)
    {
        if (sequence.GetType().GetProperty("IsDefault")?.GetValue(sequence) is true) yield break;
        var index = 0;
        foreach (var item in sequence)
        {
            if (item is not null) yield return (item, $"{path}[{index}]");
            index++;
        }
    }

    static bool HasSemanticMembers(Type type) => type.Namespace?.StartsWith("Cratis.Screenplay.Semantics", StringComparison.Ordinal) == true ||
        (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>));
}
#endif
