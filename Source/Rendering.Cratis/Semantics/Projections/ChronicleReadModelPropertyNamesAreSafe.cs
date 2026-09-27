// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Refuses read-model names whose Chronicle path or generated CLR name can point at a different property.
/// </summary>
internal static class ChronicleReadModelPropertyNamesAreSafe
{
    internal static bool Check(IReadOnlyList<SemanticProperty> properties) =>
        properties.All(property => Safe(property.Name) &&
            (property.IsIdentifier || !string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase))) &&
        properties.Select(property => GeneratedCamelCase(property.Name)).Distinct(StringComparer.Ordinal).Count() == properties.Count;

    static bool Safe(string name) => name.Length > 0 &&
        !name.Contains('.') && !name.Contains('[') && !name.Contains(']') &&
        !name.Contains('$') && !name.Contains('(') && !name.Contains(')');

    // Keep this normalization aligned with Identifiers.ToCamelCase, used for generated read-model members.
    static string GeneratedCamelCase(string name)
    {
        var pascal = string.Concat(name.Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        if (pascal.Length == 0)
        {
            return "item";
        }

        if (char.IsDigit(pascal[0]))
        {
            pascal = $"_{pascal}";
        }

        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }
}
