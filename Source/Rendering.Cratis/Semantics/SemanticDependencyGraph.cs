// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Accumulates references before resolving member identities to their declaration owners.</summary>
internal sealed class SemanticDependencyGraph
{
    readonly Dictionary<SemanticId, SemanticId> _owners = [];
    readonly HashSet<SemanticDependencyEdge> _references = [];
    readonly SortedSet<string> _incomplete = new(StringComparer.Ordinal);

    internal IReadOnlyList<string> IncompleteReasons => [.. _incomplete];

    internal void Incomplete(string reason) => _incomplete.Add(reason);

    internal void Declare(SemanticId identity, SemanticId owner)
    {
        if (_owners.TryGetValue(identity, out var existing) && existing != owner)
        {
            Incomplete($"Identity '{identity}' has more than one owner.");
        }
        else
        {
            _owners[identity] = owner;
        }
    }

    internal void Reference(SemanticId owner, SemanticId dependency, SemanticDependencyKind kind, string member) =>
        _references.Add(new(owner, dependency, kind, member));

    internal IReadOnlyList<SemanticDependencyEdge> Resolve()
    {
        var edges = new HashSet<SemanticDependencyEdge>();
        foreach (var reference in _references)
        {
            if (!_owners.TryGetValue(reference.Dependency, out var owner))
            {
                Incomplete($"{reference.Member}: unresolved identity '{reference.Dependency}' used by '{reference.Dependent}'.");
                continue;
            }
            if (reference.Dependent != owner) edges.Add(reference with { Dependency = owner });
        }
        return [.. edges.OrderBy(edge => edge.Dependency.ToString(), StringComparer.Ordinal)
            .ThenBy(edge => edge.Dependent.ToString(), StringComparer.Ordinal).ThenBy(edge => edge.Kind).ThenBy(edge => edge.Member, StringComparer.Ordinal)];
    }
}
