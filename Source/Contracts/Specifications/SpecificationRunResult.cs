// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications;

/// <summary>
/// Represents the result of running a single specification against its slice.
/// </summary>
/// <param name="SliceId">The identifier of the slice the specification belongs to.</param>
/// <param name="SpecificationId">The identifier of the specification that was run.</param>
/// <param name="SpecificationName">The name of the specification that was run.</param>
/// <param name="SliceType">The type of the slice the specification was run against.</param>
/// <param name="Outcome">The overall outcome of the run.</param>
/// <param name="Steps">The per-step outcomes that make up the run.</param>
/// <param name="Note">An optional note describing what was and was not verified for this slice type.</param>
public record SpecificationRunResult(
    Guid SliceId,
    Guid SpecificationId,
    string SpecificationName,
    SliceType SliceType,
    SpecificationRunOutcome Outcome,
    IReadOnlyList<SpecificationRunStep> Steps,
    string Note);
