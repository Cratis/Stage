// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Finds the concepts a projection adds into another concept that holds the same number (an on-hand total of stock quantities that adds
/// received quantities). C# has no conversion between two concepts, and Chronicle reads the added event property from a direct member
/// access that tolerates one conversion, so the target concept declares the implicit conversion from the source.
/// </summary>
internal static class SemanticProjectionConversions
{
    /// <summary>
    /// Gets the concepts that convert implicitly into the given concept.
    /// </summary>
    /// <param name="target">The target concept.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The source concepts, in declaration order, without duplicates.</returns>
    public static IReadOnlyList<SemanticConcept> Into(SemanticConcept target, SemanticApplicationContext context) =>
        [.. context.Projections.Values
            .Where(projection => projection.Scope is not null && context.ReadModels.ContainsKey(projection.ReadModel))
            .SelectMany(projection => Pairs(projection.Scope!, context.ReadModels[projection.ReadModel].Properties, context))
            .Where(pair => pair.Target == target.Id && pair.Source != target.Id)
            .Select(pair => pair.Source)
            .Distinct()
            .Where(context.Concepts.ContainsKey)
            .Select(id => context.Concepts[id])
            .Where(source => source.Primitive == target.Primitive && source.Values.IsEmpty)];

    static IEnumerable<(SemanticId Source, SemanticId Target)> Pairs(SemanticProjectionScope scope, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        foreach (var from in scope.From)
        {
            if (!context.Events.TryGetValue(from.EventContract, out var @event))
            {
                continue;
            }

            foreach (var mapping in from.Mappings.Where(mapping => mapping.Operation is SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract))
            {
                if (mapping.Source is SemanticProjectionEventProperty source &&
                    Find(source.Path, @event.Properties, context) is { Type.Kind: SemanticTypeReferenceKind.Concept } from_ &&
                    Find(mapping.Target, properties, context) is { Type.Kind: SemanticTypeReferenceKind.Concept } to)
                {
                    yield return (from_.Type.Target, to.Type.Target);
                }
            }
        }

        foreach (var nested in scope.Nested)
        {
            if (properties.SingleOrDefault(property => property.Id == nested.Property) is { } property && context.Types.TryGetValue(property.Type.Target, out var composite))
            {
                foreach (var pair in Pairs(nested.Scope, composite.Properties, context))
                {
                    yield return pair;
                }
            }
        }

        foreach (var children in scope.Children)
        {
            if (properties.SingleOrDefault(property => property.Id == children.Property) is { } property && context.Types.TryGetValue(property.Type.Target, out var element))
            {
                foreach (var pair in Pairs(children.Scope, element.Properties, context))
                {
                    yield return pair;
                }
            }
        }
    }

    static SemanticProperty? Find(ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        SemanticProperty? found = null;
        foreach (var id in path)
        {
            found = properties.SingleOrDefault(property => property.Id == id);
            if (found is null)
            {
                return null;
            }

            properties = found.Type.Kind == SemanticTypeReferenceKind.CompositeType && context.Types.TryGetValue(found.Type.Target, out var composite) ? composite.Properties : [];
        }

        return found;
    }
}
