// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using Cratis.Stage.Contracts.Projections;

namespace Cratis.Stage.Rendering.Cratis.for_ProjectionConverter.given;

/// <summary>
/// Compares the observable definition shape rather than owner, id, clock and event-sequence metadata.
/// </summary>
internal static class ProjectionShape
{
    public static IReadOnlyDictionary<string, string> Stage(ProjectionDefinition definition)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["AutoMap"] = definition.AutoMap.ToString(),
            ["SubscribesToAllEvents"] = definition.SubscribesToAllEvents.ToString()
        };
        StagePart(definition, result, string.Empty);
        return result;
    }

    public static IReadOnlyDictionary<string, string> Chronicle(object definition)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["AutoMap"] = Get(definition, "AutoMap")!.ToString()!,
            ["SubscribesToAllEvents"] = Get(definition, "SubscribesToAllEvents")!.ToString()!
        };
        ChroniclePart(definition, result, string.Empty);
        return result;
    }

    static void StagePart(ProjectionDefinition definition, IDictionary<string, string> result, string prefix)
    {
        foreach (var (key, from) in definition.From)
        {
            From(result, $"{prefix}From.{Event(key)}", from.Key, from.ParentKey, from.Properties);
        }
        foreach (var (key, join) in definition.Join)
        {
            Join(result, $"{prefix}Join.{Event(key)}", join.On, join.Key, join.Properties);
        }
        foreach (var (key, removal) in definition.RemovedWith)
        {
            result.Add($"{prefix}RemovedWith.{Event(key)}", $"{removal.Key}|{removal.ParentKey ?? "<null>"}");
        }
        foreach (var (key, removal) in definition.RemovedWithJoin)
        {
            result.Add($"{prefix}RemovedWithJoin.{Event(key)}", removal.Key);
        }
        Every(result, prefix, definition.FromEvery.Properties, definition.FromEvery.IncludeChildren, definition.FromEvery.AutoMap.ToString());
        foreach (var (key, child) in definition.Children)
        {
            StageChild(child, result, $"{prefix}Children.{key}.");
        }
        foreach (var (key, child) in definition.Nested ?? new Dictionary<string, ChildrenDefinition>())
        {
            StageChild(child, result, $"{prefix}Nested.{key}.");
        }
    }

    static void StageChild(ChildrenDefinition child, IDictionary<string, string> result, string prefix)
    {
        result[$"{prefix}IdentifiedBy"] = child.IdentifiedBy;
        result[$"{prefix}AutoMap"] = child.AutoMap.ToString();
        StagePart(
            new ProjectionDefinition(
                true,
                false,
                "{}",
                child.From,
                child.Join,
                child.Children,
                [],
                child.All,
                child.FromEventProperty,
                child.RemovedWith,
                child.RemovedWithJoin,
                [],
                child.AutoMap) { Nested = child.Nested },
            result,
            prefix);
    }

    static void ChroniclePart(object definition, IDictionary<string, string> result, string prefix)
    {
        foreach (var (key, from) in Entries(Get(definition, "From")))
        {
            From(result, $"{prefix}From.{Event(key)}", Value(Get(from, "Key")), NullableValue(Get(from, "ParentKey")), Mappings(Get(from, "Properties")));
        }
        foreach (var (key, join) in Entries(Get(definition, "Join")))
        {
            Join(result, $"{prefix}Join.{Event(key)}", Value(Get(join, "On")), Value(Get(join, "Key")), Mappings(Get(join, "Properties")));
        }
        foreach (var (key, removal) in Entries(Get(definition, "RemovedWith")))
        {
            result.Add($"{prefix}RemovedWith.{Event(key)}", $"{Value(Get(removal, "Key"))}|{NullableValue(Get(removal, "ParentKey")) ?? "<null>"}");
        }
        foreach (var (key, removal) in Entries(Get(definition, "RemovedWithJoin")))
        {
            result.Add($"{prefix}RemovedWithJoin.{Event(key)}", Value(Get(removal, "Key")));
        }
        var every = Get(definition, prefix.Length == 0 ? "FromEvery" : "All")!;
        Every(result, prefix, Mappings(Get(every, "Properties")), (bool)Get(every, "IncludeChildren")!, Get(every, "AutoMap")!.ToString()!);
        foreach (var (key, child) in Entries(Get(definition, "Children")))
        {
            ChronicleChild(child, result, $"{prefix}Children.{Value(key)}.");
        }
        foreach (var (key, child) in Entries(Get(definition, "Nested")))
        {
            ChronicleChild(child, result, $"{prefix}Nested.{Value(key)}.");
        }
    }

    static void ChronicleChild(object child, IDictionary<string, string> result, string prefix)
    {
        result[$"{prefix}IdentifiedBy"] = Value(Get(child, "IdentifiedBy"));
        result[$"{prefix}AutoMap"] = Get(child, "AutoMap")!.ToString()!;
        ChroniclePart(child, result, prefix);
    }

    static void From(IDictionary<string, string> result, string prefix, string? key, string? parentKey, IEnumerable<PropertyMapping> mappings)
    {
        result.Add($"{prefix}.Key", key ?? "<null>");
        result.Add($"{prefix}.ParentKey", parentKey ?? "<null>");
        Properties(result, prefix, mappings);
    }

    static void Join(IDictionary<string, string> result, string prefix, string on, string? key, IEnumerable<PropertyMapping> mappings)
    {
        result.Add($"{prefix}.On", on);
        result.Add($"{prefix}.Key", key ?? "<null>");
        Properties(result, prefix, mappings);
    }

    static void Every(IDictionary<string, string> result, string prefix, IEnumerable<PropertyMapping> mappings, bool includeChildren, string autoMap)
    {
        result.Add($"{prefix}Every.IncludeChildren", includeChildren.ToString());
        result.Add($"{prefix}Every.AutoMap", autoMap);
        Properties(result, $"{prefix}Every", mappings);
    }

    static void Properties(IDictionary<string, string> result, string prefix, IEnumerable<PropertyMapping> mappings)
    {
        foreach (var mapping in mappings)
        {
            result.Add($"{prefix}.Properties.{mapping.Property}", mapping.Expression);
        }
    }

    static IEnumerable<PropertyMapping> Mappings(object? dictionary) =>
        Entries(dictionary).Select(entry => new PropertyMapping(Value(entry.Key), (string)entry.Value));

    static IEnumerable<(object Key, object Value)> Entries(object? dictionary)
    {
        if (dictionary is null)
        {
            yield break;
        }

        var entries = ((IDictionary)dictionary).GetEnumerator();
        while (entries.MoveNext())
        {
            yield return (entries.Key, entries.Value!);
        }
    }

    static object? Get(object instance, string name) => instance.GetType().GetProperty(name)!.GetValue(instance);
    static string Value(object? value) => value is null ? string.Empty : GetValue(value) ?? value.ToString()!;
    static string? NullableValue(object? value) => value is null ? null : Value(value);
    static string? GetValue(object value) => value.GetType().GetProperty("Value")?.GetValue(value)?.ToString();
    static string Event(string value) => value.Contains('+', StringComparison.Ordinal) ? value : $"{value}+1";
    static string Event(object value) => Convert.ToString(value, CultureInfo.InvariantCulture)!;
}
