// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Keeps personal-data- or secret-containing command inputs out of Arc's property-level causation capture.
/// </summary>
/// <param name="context">The indexed semantic application.</param>
internal sealed class SemanticCommandCompliance(SemanticApplicationContext context)
{
    /// <summary>
    /// Gets a property-targeted exclusion for an input containing any compliance-marked concept, including through composites.
    /// </summary>
    /// <param name="type">The input type, including collection and optional modifiers.</param>
    /// <returns>The exclusion attribute prefix, or an empty string for other inputs.</returns>
    public string InputAttribute(SemanticTypeReference type) => ContainsProtectedValue(type, [])
        ? "[property: global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute] "
        : string.Empty;

    bool ContainsProtectedValue(SemanticTypeReference type, HashSet<SemanticId> visited) => type.Kind switch
    {
        SemanticTypeReferenceKind.Concept => context.Concepts[type.Target].Compliance is not null,
        SemanticTypeReferenceKind.CompositeType => CompositeContainsProtectedValue(type.Target, visited),
        _ => false
    };

    bool CompositeContainsProtectedValue(SemanticId id, HashSet<SemanticId> visited)
    {
        if (!visited.Add(id)) return false;

        // Modifiers retain the same target: a nullable collection still contains its element's protected values.
        return context.Types[id].Properties.Any(property => ContainsProtectedValue(property.Type, visited));
    }
}
