// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

static class ArtifactRenderInventoryGraph
{
    internal static void Validate(ImmutableArray<ArtifactRenderConstruct> constructs)
    {
        var nodes = constructs.SelectMany(construct => construct.Degradations.Select(gap => (Key: new ArtifactRenderGapReference(construct.Identity, gap.Evidence.Diagnostic.Code), Gap: gap)))
            .ToDictionary(node => node.Key, node => node.Gap);

        // Validate every target before the first graph traversal. Missing targets never escape as dictionary errors.
        foreach (var gap in nodes.Values)
        {
            foreach (var root in gap.Roots)
            {
                if (!nodes.ContainsKey(root)) throw new InvalidArtifactRenderContract("A degradation references a missing owner/code cause.");
            }
        }
        CheckAcyclic(nodes);
    }

    static void CheckAcyclic(Dictionary<ArtifactRenderGapReference, ArtifactRenderConstructGap> nodes)
    {
        var remaining = nodes.ToDictionary(node => node.Key, node => node.Value.Roots.Length);
        var dependents = Dependents(nodes);
        var ready = new Queue<ArtifactRenderGapReference>(remaining.Where(node => node.Value == 0).Select(node => node.Key));
        var visited = 0;
        while (ready.TryDequeue(out var key))
        {
            visited++;
            foreach (var dependent in dependents[key])
            {
                if (--remaining[dependent] == 0) ready.Enqueue(dependent);
            }
        }
        if (visited != nodes.Count) throw new InvalidArtifactRenderContract("Degradation causes contain a cycle.");
    }

    static Dictionary<ArtifactRenderGapReference, List<ArtifactRenderGapReference>> Dependents(Dictionary<ArtifactRenderGapReference, ArtifactRenderConstructGap> nodes)
    {
        var dependents = nodes.Keys.ToDictionary(key => key, _ => new List<ArtifactRenderGapReference>());
        foreach (var node in nodes)
        {
            foreach (var root in node.Value.Roots) dependents[root].Add(node.Key);
        }
        return dependents;
    }
}
