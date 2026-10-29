// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Indexes ESM references independently of rendering support. An incomplete index must never authorize omission.
/// Screens/navigation require syntax bindings unavailable here; seeds are host syntax, not ESM. Neither is covered.
/// </summary>
internal sealed class SemanticDependencyIndex
{
    readonly Dictionary<SemanticId, SemanticId[]> _dependents;

    /// <summary>Builds a deterministic snapshot without modifying the application or its execution plan.</summary>
    /// <param name="context">The application whose references are indexed.</param>
    internal SemanticDependencyIndex(SemanticApplicationContext context)
    {
        var graph = new SemanticDependencyGraph();
        var builder = new SemanticDependencyBuilder(context, graph);
        builder.Build();
        Edges = graph.Resolve();
        IncompleteReasons = graph.IncompleteReasons;
        Incompleteness = graph.Incompleteness;
        _dependents = Edges.GroupBy(edge => edge.Dependency).ToDictionary(
            group => group.Key,
            group => group.Select(edge => edge.Dependent).Distinct().OrderBy(id => id.ToString(), StringComparer.Ordinal).ToArray());
    }

    internal static IReadOnlyList<string> OutOfIndex { get; } =
    [
        "Screens/navigation: syntax bindings are not carried by SemanticApplicationContext; dependent-scene safety is unknown.",
        "Seeds: host syntax outside ESM; no seed graph is claimed."
    ];

    internal IReadOnlyList<SemanticDependencyEdge> Edges { get; }

    internal IReadOnlyList<string> IncompleteReasons { get; }

    internal IReadOnlyList<SemanticDependencyIncompleteness> Incompleteness { get; }

    // Only ESM reference coverage: never proof of Scene/navigation closure or full runtime dependency safety.
    internal bool IsSemanticGraphComplete => IncompleteReasons.Count == 0;

    /// <summary>Finds transitive dependents, excluding the queried owner even when references form a cycle.</summary>
    /// <param name="owner">The omitted or changed declaration owner.</param>
    /// <returns>Distinct dependent owners in semantic identity order.</returns>
    internal IReadOnlyList<SemanticId> Dependents(SemanticId owner)
    {
        var visited = new HashSet<SemanticId> { owner };
        var pending = new Queue<SemanticId>([owner]);
        while (pending.TryDequeue(out var current))
        {
            if (!_dependents.TryGetValue(current, out var dependents)) continue;
            foreach (var dependent in dependents.Where(visited.Add)) pending.Enqueue(dependent);
        }
        visited.Remove(owner);
        return [.. visited.OrderBy(id => id.ToString(), StringComparer.Ordinal)];
    }
}
