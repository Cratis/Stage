// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Specifications;

namespace Cratis.Stage.Running;

/// <summary>
/// Runs specifications for automation and translator slices (a reactor reacts to events and produces side
/// effects or follow-up events). The reactor logic itself is not modeled, so verification is structural.
/// </summary>
public sealed class ReactorRunStrategy : ISpecificationRunStrategy
{
    const string Note =
        "Automation/translation: verified the Given/Then events resolve to the slice. The reactor logic is " +
        "not modeled, so behavioral execution of the side effects is a follow-up.";

    /// <inheritdoc/>
    public SliceType SliceType => SliceType.Automation;

    /// <inheritdoc/>
    public SpecificationRunResult Run(Slice slice, Specification specification)
    {
        var given = SpecificationVerification.VerifyGivenEvents(slice, specification);
        var then = SpecificationVerification.VerifyThenEvents(slice, specification);

        var structural = new SpecificationRunStep(
            SpecificationRunStepKind.Structural,
            "Reactor behavior",
            given.Outcome == SpecificationRunOutcome.Failed || then.Outcome == SpecificationRunOutcome.Failed
                ? SpecificationRunOutcome.Failed
                : SpecificationRunOutcome.Inconclusive,
            "Reactor behavior is not modeled and was not executed.",
            []);

        IReadOnlyList<SpecificationRunStep> steps = [given, then, structural];

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
