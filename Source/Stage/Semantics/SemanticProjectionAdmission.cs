// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Semantics;

// Replay keeps all facts and all other behavior. Only projections refused by public scoped
// compilation are removed from a separate plan; live admission always uses the original plan.
internal static class SemanticProjectionAdmission
{
    static readonly ConditionalWeakTable<SemanticExecutionPlan, SemanticExecutionPlan> _replayPlans = [];

    internal static SemanticExecutionPlan ForReplay(SemanticExecutionPlan plan) =>
        _replayPlans.GetValue(plan, CreateReplayPlan);

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

    static SemanticExecutionPlan CreateReplayPlan(SemanticExecutionPlan plan)
    {
        var excluded = Unadmitted(plan).Select(projection => projection.Id).ToHashSet();
        if (excluded.Count == 0 || !SemanticPlanAdmission.Compilation(plan).Success) return plan;

        var application = plan.Model.Application with
        {
            Modules = [.. plan.Model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => WithoutProjections(feature, excluded))]
            })]
        };
        var model = ExecutableSemanticModel.Create(plan.Model.LanguageVersion, plan.Model.SemanticVersion, application);
        var compiled = SemanticExecutionPlan.Compile(model, []);
        if (!compiled.Success)
        {
            throw new InvalidSemanticContract(string.Join("; ", compiled.Issues.Select(issue => issue.Details)));
        }
        return compiled.Plan!;
    }

    static SemanticFeature WithoutProjections(SemanticFeature feature, HashSet<SemanticId> excluded) => feature with
    {
        Features = [.. feature.Features.Select(child => WithoutProjections(child, excluded))],
        Slices = [.. feature.Slices.Select(slice => slice with
        {
            Projections = [.. slice.Projections.Where(projection => !excluded.Contains(projection.Id))]
        })]
    };
}
