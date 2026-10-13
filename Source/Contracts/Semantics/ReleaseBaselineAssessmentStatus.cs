// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// Describes the outcome of a nonblocking release-baseline assessment.
/// </summary>
public enum ReleaseBaselineAssessmentStatus
{
    /// <summary>
    /// No assessment was supplied; rendering remains unchecked.
    /// </summary>
    NotSupplied,

    /// <summary>
    /// The model root has no release-baseline sidecar.
    /// </summary>
    Absent,

    /// <summary>
    /// All admitted revisions were checked against authoritative identities.
    /// </summary>
    Checked,

    /// <summary>
    /// The baseline is malformed, unsupported, or belongs to another application.
    /// </summary>
    Unreadable,

    /// <summary>
    /// The model or its identity provenance does not permit a check.
    /// </summary>
    Unavailable
}
