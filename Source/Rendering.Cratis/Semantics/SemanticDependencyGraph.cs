// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Accumulates references before resolving member identities to their declaration owners.</summary>
internal sealed class SemanticDependencyGraph
{
    readonly Dictionary<SemanticId, SemanticId> _owners = [];
    readonly HashSet<SemanticDependencyEdge> _references = [];
    readonly Dictionary<SemanticDependencyEdge, SemanticId?> _propertyTypes = [];
    readonly HashSet<SemanticDependencyIncompleteness> _incomplete = [];

    internal IReadOnlyList<string> IncompleteReasons => [.. _incomplete.Select(gap => gap.Reason).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    internal IReadOnlyList<SemanticDependencyIncompleteness> Incompleteness => [.. _incomplete.OrderBy(gap => gap.Reason, StringComparer.Ordinal).ThenBy(gap => gap.Owner?.ToString(), StringComparer.Ordinal)];

    internal void Incomplete(SemanticDependencyIncompleteness incompleteness) => _incomplete.Add(incompleteness);

    internal void Declare(SemanticId identity, SemanticId owner)
    {
        if (_owners.TryGetValue(identity, out var existing) && existing != owner)
        {
            Incomplete(new(SemanticDependencyIncompletenessKind.DuplicateOwner, null, string.Empty, null, $"Identity '{identity}' has more than one owner."));
        }
        else
        {
            _owners[identity] = owner;
        }
    }

    internal void Reference(SemanticId owner, SemanticId dependency, SemanticDependencyKind kind, string member) => Reference(new(owner, dependency, kind, member));

    internal void Reference(SemanticDependencyEdge reference, SemanticId? propertyTypeOf = null)
    {
        _references.Add(reference);
        _propertyTypes[reference] = propertyTypeOf;
    }

    internal IReadOnlyList<SemanticDependencyEdge> Resolve()
    {
        var edges = new HashSet<SemanticDependencyEdge>();
        foreach (var reference in _references)
        {
            if (!_owners.TryGetValue(reference.Dependency, out var owner))
            {
                Incomplete(new(SemanticDependencyIncompletenessKind.UnresolvedIdentity, reference.Dependent, reference.Member, _propertyTypes[reference], $"{reference.Member}: unresolved identity '{reference.Dependency}' used by '{reference.Dependent}'."));
                continue;
            }
            if (reference.Dependent != owner) edges.Add(reference with { Dependency = owner });
        }
        return [.. edges.OrderBy(edge => edge.Dependency.ToString(), StringComparer.Ordinal)
            .ThenBy(edge => edge.Dependent.ToString(), StringComparer.Ordinal).ThenBy(edge => edge.Kind).ThenBy(edge => edge.Member, StringComparer.Ordinal)];
    }
}
