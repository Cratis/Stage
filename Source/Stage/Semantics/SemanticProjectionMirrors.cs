// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Projections;
using Cratis.Stage.Runtime;
using ChronicleProjections = Cratis.Chronicle.Contracts.Projections;
using ChronicleReadModels = Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Lowers only complete flat semantic projections to the equivalent Chronicle mirror.
/// </summary>
public static class SemanticProjectionMirrors
{
    /// <summary>
    /// Tries to lower one projection without approximating unsupported mappings.
    /// </summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="semantic">The semantic projection.</param>
    /// <param name="readModel">The equivalent Chronicle read model when supported.</param>
    /// <param name="projection">The equivalent Chronicle projection when supported.</param>
    /// <param name="reason">The reason when it cannot be lowered.</param>
    /// <returns>Whether an exact mirror was built.</returns>
    public static bool TryLower(
        SemanticExecutionPlan plan,
        SemanticProjection semantic,
        out ChronicleReadModels.ReadModelDefinition? readModel,
        out ChronicleProjections.ProjectionDefinition? projection,
        out string reason)
    {
        readModel = null;
        projection = null;
        reason = "Only complete flat, single-key property transitions can be mirrored exactly.";
        if (plan.Projections.Values.Count(candidate => candidate.ReadModel == semantic.ReadModel) != 1 ||
            semantic.Scope is not null || semantic.Transitions.IsEmpty ||
            semantic.Transitions.Select(transition => transition.EventContract).Distinct().Count() != semantic.Transitions.Length)
        {
            return false;
        }

        var target = plan.ReadModels[semantic.ReadModel];
        if (target.Properties.Any(property => property.Type.IsOptional))
        {
            return false;
        }

        var identifier = target.Properties.Single(property => property.IsIdentifier);
        var from = new Dictionary<string, FromDefinition>(StringComparer.Ordinal);
        foreach (var transition in semantic.Transitions)
        {
            var source = plan.Events[transition.EventContract];
            if (transition.AffectedInstance.Cardinality != AffectedInstanceCardinality.One ||
                transition.AffectedInstance.Key is not SemanticResolvedExpression
                {
                    Root: SemanticExpressionRootKind.Event,
                    Source: SemanticExpressionSourceKind.Property
                } key ||
                source.Properties.All(property => property.Id != key.Target || property.Type.IsOptional) ||
                transition.Mappings.Select(mapping => mapping.TargetProperty).Distinct().Count() != target.Properties.Length ||
                !transition.Mappings.Any(mapping => mapping.TargetProperty == identifier.Id && mapping.Source is SemanticResolvedExpression
                {
                    Root: SemanticExpressionRootKind.Event,
                    Target: var sourceId
                } && sourceId == key.Target))
            {
                return false;
            }

            var mappings = new List<PropertyMapping>();
            foreach (var mapping in transition.Mappings)
            {
                var property = target.Properties.SingleOrDefault(candidate => candidate.Id == mapping.TargetProperty);
                var expression = mapping.Source switch
                {
                    SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } resolved =>
                        source.Properties.SingleOrDefault(candidate => candidate.Id == resolved.Target)?.Name,
                    SemanticValueExpression { Value: SemanticTextValue literal } => $"$value({literal.Value})",
                    _ => null
                };
                if (property is null || expression is null)
                {
                    return false;
                }

                try
                {
                    ProjectionRuntimeExpression.Translate(expression);
                }
                catch (UnsupportedProjectionRuntimeExpression)
                {
                    return false;
                }

                mappings.Add(new(property.Name, expression));
            }

            var keyName = source.Properties.Single(property => property.Id == key.Target).Name;
            try
            {
                ProjectionRuntimeKey.Translate(keyName);
            }
            catch (UnsupportedProjectionRuntimeExpression)
            {
                return false;
            }

            from.Add(source.Name, new(mappings, keyName));
        }

        var definition = new ProjectionDefinition(
            true,
            true,
            "{}",
            from,
            new Dictionary<string, JoinDefinition>(),
            new Dictionary<string, ChildrenDefinition>(),
            [],
            new FromEveryDefinition([]),
            null,
            new Dictionary<string, RemovedWithDefinition>(),
            new Dictionary<string, RemovedWithJoinDefinition>(),
            [],
            ProjectionAutoMap.Disabled);
        var readModelId = StageChronicleDefinitions.DeterministicGuid(target.Id.ToString());
        var projectionId = StageChronicleDefinitions.DeterministicGuid($"{readModelId}:projection").ToString();
        var readModelDefinition = new ReadModelDefinition(
            readModelId,
            target.Name,
            SemanticSchemas.Schema(target.Properties, plan.Model.Application),
            definition);
        readModel = StageChronicleDefinitions.BuildReadModel(readModelDefinition, readModelId.ToString(), projectionId);
        projection = StageChronicleDefinitions.BuildProjection(definition, projectionId, readModelId.ToString(), Cratis.Chronicle.EventSequences.EventSequenceId.Log);
        reason = string.Empty;
        return true;
    }
}
