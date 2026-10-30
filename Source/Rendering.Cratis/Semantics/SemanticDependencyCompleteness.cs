// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Proves reference coverage outside the deliberately omitted events' own property types.</summary>
internal static class SemanticDependencyCompleteness
{
    static readonly HashSet<string> _members = new(StringComparer.Ordinal)
    {
        "SemanticProperty.Type",
        "SemanticTypeReference.Kind",
        "SemanticTypeReference.Primitive",
        "SemanticTypeReference.Target"
    };

    internal static bool Tolerates(SemanticDependencyIndex index, IReadOnlyDictionary<SemanticId, string> roots, SemanticApplicationContext context)
    {
        var rootOwners = roots.Keys.ToHashSet();
        var eventOwners = context.Events.Keys.ToHashSet();
        return index.Incompleteness.All(gap => IsRootLocal(gap, rootOwners, eventOwners));
    }

    internal static bool IsRootLocal(SemanticDependencyIncompleteness gap, IReadOnlySet<SemanticId> roots, IReadOnlySet<SemanticId> events) =>
        EligibleLocation(gap) && OwnPropertyOfEvent(gap, roots, events);

    static bool EligibleLocation(SemanticDependencyIncompleteness gap) =>
        gap.Kind is SemanticDependencyIncompletenessKind.UnknownVariant or SemanticDependencyIncompletenessKind.UnresolvedIdentity && _members.Contains(gap.Member);

    static bool OwnPropertyOfEvent(SemanticDependencyIncompleteness gap, IReadOnlySet<SemanticId> roots, IReadOnlySet<SemanticId> events)
    {
        if (gap.Owner is not { } owner) return false;
        return roots.Contains(owner) && events.Contains(owner) && gap.PropertyTypeOf == owner;
    }
}
