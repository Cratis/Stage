// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Resolves the declared property and primitive of an authorization claim target.
/// </summary>
internal static class SemanticClaimTargets
{
    internal static SemanticProperty? Property(SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, string path)
    {
        SemanticProperty? property = null;
        foreach (var segment in path.Split('.'))
        {
            property = properties.SingleOrDefault(candidate => candidate.Name == segment);
            if (property is null)
            {
                return null;
            }

            properties = property.Type.Kind == SemanticTypeReferenceKind.CompositeType && context.Types.TryGetValue(property.Type.Target, out var composite)
                ? composite.Properties : [];
        }

        return property;
    }

    internal static SemanticPrimitiveType Primitive(SemanticApplicationContext context, SemanticProperty? property) => property?.Type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => property.Type.Primitive,
        SemanticTypeReferenceKind.Concept => context.Concepts[property.Type.Target].Primitive,
        _ => SemanticPrimitiveType.Unknown
    };
}
