// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Specifications;

namespace Cratis.Stage.Running;

/// <summary>
/// Runs specifications for state change slices (command appends events).
/// </summary>
public sealed class StateChangeRunStrategy : ISpecificationRunStrategy
{
    const string Note =
        "State change: verified the When command, the Given/Then events resolve to the slice, and the " +
        "modeled required rules are consistent with the modeled Then errors. Behavioral execution of the " +
        "command against a live Chronicle is a follow-up.";

    /// <inheritdoc/>
    public SliceType SliceType => SliceType.StateChange;

    /// <inheritdoc/>
    public SpecificationRunResult Run(Slice slice, Specification specification)
    {
        IReadOnlyList<SpecificationRunStep> steps =
        [
            SpecificationVerification.VerifyGivenEvents(slice, specification),
            SpecificationVerification.VerifyWhenCommand(slice, specification),
            SpecificationVerification.VerifyThenEvents(slice, specification),
            SpecificationVerification.VerifyThenErrors(slice, specification),
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
