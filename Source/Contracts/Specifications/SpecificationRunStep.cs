// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications;

/// <summary>
/// Represents the outcome of one step within a specification run.
/// </summary>
/// <param name="Kind">The part of the given/when/then specification this step corresponds to.</param>
/// <param name="Title">A human-readable title for the step.</param>
/// <param name="Outcome">The outcome of the step.</param>
/// <param name="Message">An optional message describing the outcome (for example a failure reason).</param>
/// <param name="Differences">The value mismatches found in this step; empty when the step passed.</param>
public record SpecificationRunStep(
    SpecificationRunStepKind Kind,
    string Title,
    SpecificationRunOutcome Outcome,
    string? Message,
    IReadOnlyList<SpecificationValueDifference> Differences);
