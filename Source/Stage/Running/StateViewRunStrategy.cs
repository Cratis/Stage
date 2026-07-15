// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Specifications;

namespace Cratis.Stage.Running;

/// <summary>
/// Runs specifications for state view slices (events project into a read model).
/// </summary>
public sealed class StateViewRunStrategy : ISpecificationRunStrategy
{
    const string Note =
        "State view: verified the Given events resolve to the slice and the slice defines a read model. " +
        "Behavioral execution of the projection against a live Chronicle to compare the resulting read-model " +
        "state is a follow-up.";

    /// <inheritdoc/>
    public SliceType SliceType => SliceType.StateView;

    /// <inheritdoc/>
    public SpecificationRunResult Run(Slice slice, Specification specification)
    {
        var readModelStep = slice.ReadModel is not null
            ? new SpecificationRunStep(
                SpecificationRunStepKind.ThenReadModel,
                $"Then read model {slice.ReadModel.Name}",
                SpecificationRunOutcome.Inconclusive,
                "Projection execution is not yet available; the read-model state was not compared.",
                [])
            : new SpecificationRunStep(
                SpecificationRunStepKind.ThenReadModel,
                "Then read model",
                SpecificationRunOutcome.Failed,
                "The slice defines no read model to project into.",
                []);

        IReadOnlyList<SpecificationRunStep> steps =
        [
            SpecificationVerification.VerifyGivenEvents(slice, specification),
            readModelStep,
        ];

        return new SpecificationRunResult(
            slice.Id,
            specification.Id,
            specification.Name,
            slice.SliceType,
            SpecificationVerification.Aggregate(steps),
            steps,
            Note);
    }
}
