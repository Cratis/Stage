// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Semantics;

// Admission follows productions through reactions to projections refused by scoped compilation.
internal static class SemanticProjectionAdmission
{
    internal static SemanticUnsupportedCapability? CheckProductions(SemanticExecutionPlan plan, SemanticId artifact)
    {
        IEnumerable<SemanticId> initial;
        if (plan.Commands.TryGetValue(artifact, out var command))
        {
            initial = command.Produces.Select(produced => produced.EventContract);
        }
        else if (plan.Reactions.FirstOrDefault(candidate => candidate.Id == artifact) is { } reaction)
        {
            initial = SemanticRunReactionAdmission.Productions(plan, reaction);
        }
        else
        {
            return null;
        }

        var projections = Unadmitted(plan);
        if (projections.Length == 0) return null;

        var events = SemanticRunReactionAdmission.ProductionClosure(plan, initial);
        var projection = projections.FirstOrDefault(candidate => events.Any(@event => Observes(candidate, @event)));
        return projection is null ? null : new(StageExecutionCapability.Projection, artifact.ToString(),
            $"STAGE-PLAN-004: Projection '{projection.Name}' was not admitted by scoped execution-plan compilation; this artifact's productions reach it.");
    }

    static SemanticProjection[] Unadmitted(SemanticExecutionPlan plan) =>
        [.. SemanticPlanAdmission.Compilation(plan).UnselectedIssues
            .Where(issue => plan.Projections.ContainsKey(issue.Artifact))
            .Select(issue => plan.Projections[issue.Artifact]).DistinctBy(projection => projection.Id)
            .OrderBy(projection => projection.Id.ToString(), StringComparer.Ordinal)];

    static bool Observes(SemanticProjection projection, SemanticId @event) =>
        projection.Transitions.Any(transition => transition.EventContract == @event) ||
        (projection.Scope is { } scope && Observes(scope, @event));

    static bool Observes(SemanticProjectionScope scope, SemanticId @event) =>
        scope.Every is not null || scope.From.Any(from => from.EventContract == @event) ||
        scope.Joins.Any(join => join.EventContract == @event) ||
        scope.Removals.Any(removal => removal.EventContract == @event) ||
        scope.JoinRemovals.Any(removal => removal.EventContract == @event) ||
        scope.Children.Any(child => Observes(child.Scope, @event)) ||
        scope.Nested.Any(nested => Observes(nested.Scope, @event));

}
