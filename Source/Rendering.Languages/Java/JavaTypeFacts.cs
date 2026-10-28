// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Holds immutable deep inheritable names keyed by canonical dotted type name, including dotted nested types.
/// </summary>
/// <remarks>
/// Deep names include accessible member types and non-private fields of a type user code can extend or implement,
/// plus the deep names of every accessible member type. An absent entry is unknown, never known-empty.
/// </remarks>
public sealed class JavaTypeFacts
{
    readonly ImmutableDictionary<string, ImmutableHashSet<string>> _types;

    internal JavaTypeFacts(ImmutableDictionary<string, ImmutableHashSet<string>> types) => _types = types;

    /// <summary>
    /// Gets the bundled union of the JDK 21 and 25 facts, documented in JavaTypeFacts.Bundled.md.
    /// </summary>
    public static JavaTypeFacts Bundled { get; } = JavaBundledTypeFacts.Create();

    /// <summary>
    /// Parses the strict types-object format, removing identifier-ignorable characters from member names.
    /// </summary>
    /// <param name="json">An object whose sole property is types, mapping canonical type names to arrays of member names.</param>
    /// <returns>The immutable facts.</returns>
    /// <exception cref="JavaTypeFactsException">The JSON shape, a property, a canonical type segment or a member name is invalid.</exception>
    public static JavaTypeFacts Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return new(ReadTypes(document.RootElement));
        }
        catch (JsonException error)
        {
            throw new JavaTypeFactsException($"Invalid type-facts JSON: {error.Message}");
        }
    }

    /// <summary>
    /// Unions the deep names for each canonical type without modifying either operand.
    /// </summary>
    /// <param name="other">The facts to merge.</param>
    /// <returns>The union, keeping known-empty entries.</returns>
    public JavaTypeFacts Union(JavaTypeFacts other)
    {
        var types = _types.ToBuilder();
        foreach (var (type, names) in other._types)
        {
            types[type] = types.TryGetValue(type, out var existing) ? existing.Union(names) : names;
        }

        return new(types.ToImmutable());
    }

    /// <summary>
    /// Looks up the deep inheritable names of a canonical type.
    /// </summary>
    /// <param name="fqn">The canonical dotted name.</param>
    /// <param name="names">The immutable names, or an empty set when unknown; the return value distinguishes unknown from known-empty.</param>
    /// <returns>Whether the type is known.</returns>
    public bool TryGet(string fqn, out ImmutableHashSet<string> names)
    {
        var known = _types.TryGetValue(fqn, out var found);
        names = found ?? ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal);
        return known;
    }

    static ImmutableDictionary<string, ImmutableHashSet<string>> ReadTypes(JsonElement root)
    {
        Require(root.ValueKind == JsonValueKind.Object, "Type facts must be an object.");
        var properties = root.EnumerateObject().ToArray();
        Require(properties.Length == 1 && properties[0].Name == "types", "Type facts must have exactly one property, 'types'.");
        var types = properties[0].Value;
        Require(types.ValueKind == JsonValueKind.Object, "The 'types' value must be an object.");
        var result = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<string>>(StringComparer.Ordinal);
        foreach (var property in types.EnumerateObject())
        {
            Require(property.Name.Split('.').Length >= 2 && property.Name.Split('.').All(JavaIdentifiers.IsSymbolSegment), $"Invalid canonical type name '{property.Name}'.");
            Require(!result.ContainsKey(property.Name), $"Duplicate type '{property.Name}'.");
            result.Add(property.Name, ReadNames(property.Value));
        }

        return result.ToImmutable();
    }

    static ImmutableHashSet<string> ReadNames(JsonElement element)
    {
        Require(element.ValueKind == JsonValueKind.Array, "A type's names must be an array.");
        var names = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (var value in element.EnumerateArray())
        {
            Require(value.ValueKind == JsonValueKind.String, "A member name must be a string.");
            var name = JavaIdentifierIgnorables.Remove(value.GetString()!);
            Require(JavaIdentifiers.IsIdentifier(name), $"Invalid member name '{name}'.");
            names.Add(name);
        }

        return names.ToImmutable();
    }

    static void Require(bool valid, string message)
    {
        if (!valid)
        {
            throw new JavaTypeFactsException(message);
        }
    }
}
