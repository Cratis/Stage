// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.Comparison;

/// <summary>
/// Folds a specification's facts through the admitted reducer bodies, the same way a live restart replays the log.
/// </summary>
/// <remarks>
/// A reducer that cannot run, or a history the reference evaluator refuses, refuses the read model: the run never invents
/// or drops state the reducer did not produce.
/// </remarks>
internal static class SemanticRunReducers
{
    internal static IReadOnlyList<SemanticRunProjections.Projected> Fold(SemanticExecutionPlan plan, SemanticId readModel, SemanticRunContext context, SemanticBodyExecution bodies)
    {
        if (context.WorldFixture.GivenReadModels.Any(seed => seed.ReadModel == readModel))
        {
            throw Refused(readModel, "A reducer-backed read model cannot start from a given read-model state.");
        }

        return Project(SemanticReducerWorld.Establish(plan, Facts(plan, context), bodies), readModel);
    }

    static IReadOnlyList<SemanticRunProjections.Projected> Project(SemanticExecutionResult outcome, SemanticId readModel) =>
        outcome switch
        {
            SemanticAccepted accepted => [.. accepted.World.ReadModels
                .Where(instance => instance.ReadModel == readModel)
                .Select(instance => new SemanticRunProjections.Projected(readModel, instance.Key, instance.Values))],
            SemanticUnsupported unsupported => throw Refused(readModel, unsupported.Details),
            SemanticRejected rejected => throw Refused(readModel, rejected.Details),
            _ => throw Refused(readModel, "The reducer produced no outcome.")
        };

    static IEnumerable<SemanticFact> Facts(SemanticExecutionPlan plan, SemanticRunContext context) =>
        context.Facts.Select((fact, index) => new SemanticFact(fact.EventContract, context.Destinations[index], fact.Values)
        {
            Context = plan.Model.SemanticVersion != SemanticVersion.V1 && fact.EventSource is { } identity ? new(identity) : null,
            Route = SemanticRunRoutes.Format(plan, fact.Route),
            Occurred = context.Occurrences[index] ?? context.Occurred
        });

    static SemanticProjectionLoweringRefused Refused(SemanticId readModel, string details) =>
        new(new InvalidOperationException($"Reducer-backed read model '{readModel}': {details}")) { Projection = readModel };
}
