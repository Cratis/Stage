// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Connects constraints to every append site discovered by the semantic traversal.</summary>
internal sealed class SemanticDependencyProductions
{
    readonly Dictionary<SemanticId, HashSet<SemanticId>> _producers = [];

    internal void Register(object node, SemanticId owner)
    {
        if (ProducedContract(node) is not { } identity) return;
        if (!_producers.TryGetValue(identity, out var owners)) _producers[identity] = owners = [];
        owners.Add(owner);
    }

    internal void ConnectConstraints(SemanticApplicationContext context, SemanticDependencyGraph graph)
    {
        foreach (var located in context.AllSlices)
        {
            foreach (var constraint in located.Slice.Constraints) ConnectConstraint(constraint, located.Slice.Id, graph);
        }
    }

    static SemanticId? ProducedContract(object node) => node switch
    {
        SemanticProducedEvent produced => produced.EventContract,
        SemanticCaptureAppend append => append.EventContract,
        SemanticProjection { Target: SemanticProjectionTargetKind.Event } projection => projection.ReadModel,
        SemanticReducer { Target: SemanticProjectionTargetKind.Event } reducer => reducer.ReadModel,
        SemanticSpecificationAppend append => append.EventContract,
        _ => null
    };

    void ConnectConstraint(SemanticConstraint constraint, SemanticId owner, SemanticDependencyGraph graph)
    {
        foreach (var contract in constraint.Targets.Select(target => target.EventContract).Concat(constraint.ReleasedBy).Distinct())
        {
            if (!_producers.TryGetValue(contract, out var producers)) continue;
            foreach (var producer in producers) graph.Reference(producer, owner, SemanticDependencyKind.Constraint, "SemanticConstraint.Targets/ReleasedBy");
        }
    }
}
