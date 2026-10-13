// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// Carries a whole-application release-baseline check without affecting rendering admission.
/// </summary>
/// <param name="Status">Whether the baseline was supplied, absent, checked, unreadable, or unavailable.</param>
/// <param name="ModelRevision">The semantic revision assessed, when executable.</param>
/// <param name="Violations">Every affected admitted event revision.</param>
/// <param name="Reason">Why the baseline could not be checked.</param>
public sealed record ReleaseBaselineAssessment(
    ReleaseBaselineAssessmentStatus Status,
    SemanticRevision ModelRevision,
    ImmutableArray<StageReleaseBaselineViolation> Violations,
    string? Reason = null)
{
    /// <summary>
    /// Gets the unchecked assessment used by callers without a baseline input.
    /// </summary>
    public static ReleaseBaselineAssessment NotSupplied { get; } = new(ReleaseBaselineAssessmentStatus.NotSupplied, default, []);
}
