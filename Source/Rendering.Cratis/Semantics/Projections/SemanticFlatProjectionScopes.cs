// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Expresses a flat projection as a scoped one. Screenplay keeps the flat shape for every projection it can express, but the flat
/// rendering only takes one transition whose key is the event source; a projection with several <c language="plain">from</c> blocks, a key that is a
/// payload property of an event sourced by something else, or a constant mapping is rendered through the scope that says the same.
/// </summary>
internal static class SemanticFlatProjectionScopes
{
    /// <summary>
    /// Gets the scope that says what the flat transitions of a projection say.
    /// </summary>
    /// <param name="projection">The flat projection.</param>
    /// <returns>The equivalent scope, or <see langword="null"/> when a mapping has no scoped form.</returns>
    public static SemanticProjectionScope? ToScope(SemanticProjection projection)
    {
        var froms = ImmutableArray.CreateBuilder<SemanticProjectionFrom>();
        foreach (var transition in projection.Transitions)
        {
            if (transition.AffectedInstance.Cardinality != AffectedInstanceCardinality.One || transition.AffectedInstance.Key is not SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property, Root: SemanticExpressionRootKind.Event } key)
            {
                return null;
            }

            var mappings = ImmutableArray.CreateBuilder<SemanticProjectionMapping>();
            foreach (var mapping in transition.Mappings)
            {
                SemanticProjectionValue? source = mapping.Source switch
                {
                    SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property, Root: SemanticExpressionRootKind.Event } property =>
                        new SemanticProjectionEventProperty([property.Target]),
                    SemanticValueExpression value => new SemanticProjectionLiteral(value.Value),
                    _ => null
                };
                if (source is null)
                {
                    return null;
                }

                mappings.Add(new([mapping.TargetProperty], SemanticProjectionOperation.Set, source));
            }

            froms.Add(new(transition.EventContract, new SemanticProjectionValueKey(new SemanticProjectionEventProperty([key.Target])), null, mappings.ToImmutable()));
        }

        return SemanticProjectionScope.Empty with { From = froms.ToImmutable() };
    }
}
