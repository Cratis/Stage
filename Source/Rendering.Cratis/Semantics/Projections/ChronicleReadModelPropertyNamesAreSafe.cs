// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Refuses read-model names whose Chronicle path or generated CLR name can point at a different property.
/// </summary>
internal static class ChronicleReadModelPropertyNamesAreSafe
{
    internal static bool Check(IReadOnlyList<SemanticProperty> properties, bool rootLevel = true) =>
        properties.All(property => Safe(property.Name) && (!rootLevel || !CollidesWithRootDocumentKey(property))) &&
        properties.Select(property => GeneratedCamelCase(property.Name)).Distinct(StringComparer.Ordinal).Count() == properties.Count;

    internal static bool CollidesWithRootDocumentKey(SemanticProperty property) =>
        !property.IsIdentifier && string.Equals(GeneratedCamelCase(property.Name), "id", StringComparison.OrdinalIgnoreCase);

    static bool Safe(string name) => name.Length > 0 &&
        !name.Contains('.') && !name.Contains('[') && !name.Contains(']') &&
        !name.Contains('$') && !name.Contains('(') && !name.Contains(')');

    // Identifiers.ToCamelCase uses this same Pascal spelling for emitted read-model members.
    static string GeneratedCamelCase(string name)
    {
        var pascal = GeneratedPascalCase.From(name);
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }
}
