// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Folds a fact history through the renderer-admitted reducer bodies, exactly as a live restart replays the event log.
/// </summary>
public static class SemanticReducerWorld
{
    /// <summary>
    /// Finds whether a reducer builds a read model.
    /// </summary>
    /// <param name="plan">The executable semantic plan.</param>
    /// <param name="readModel">The read model.</param>
    /// <returns>Whether any reducer targets the read model.</returns>
    public static bool Reduces(SemanticExecutionPlan plan, SemanticId readModel) =>
        plan.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(Slices)
            .SelectMany(slice => slice.Reducers).Any(reducer => reducer.ReadModel == readModel);

    /// <summary>
    /// Establishes the world a history produces, executing reducer transitions in the body worker.
    /// </summary>
    /// <param name="plan">The executable semantic plan.</param>
    /// <param name="facts">The facts, in append order.</param>
    /// <param name="bodies">The compiled bodies the reducer transitions execute through.</param>
    /// <returns>The accepted world, or the explicit unsupported or rejected outcome.</returns>
    public static SemanticExecutionResult Establish(SemanticExecutionPlan plan, IEnumerable<SemanticFact> facts, SemanticBodyExecution bodies) =>
        new SemanticEvaluator(bodies, bodies.Bodies).EstablishWorld(SemanticProjectionAdmission.ForReplay(plan), [.. facts]);

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));
}
