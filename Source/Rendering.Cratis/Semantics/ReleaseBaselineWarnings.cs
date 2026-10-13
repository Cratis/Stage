// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class ReleaseBaselineWarnings
{
    internal static ImmutableArray<ArtifactRenderDiagnostic> For(ArtifactRenderRequest request)
    {
        var assessment = request.ReleaseBaseline;
        if (assessment.Status == ReleaseBaselineAssessmentStatus.NotSupplied) return [];
        if (assessment.ModelRevision != request.Model.Revision)
        {
            return [Warning(request, "STAGE-BASELINE-004", "The release-baseline assessment does not match the planned semantic revision; admitted revisions were not checked for this model.")];
        }

        return assessment.Status switch
        {
            ReleaseBaselineAssessmentStatus.Absent => [],
            ReleaseBaselineAssessmentStatus.Checked => [.. assessment.Violations.Select(violation => Violation(request, violation))],
            ReleaseBaselineAssessmentStatus.Unreadable => [Warning(request, "STAGE-BASELINE-003", $"The release baseline is unreadable: {assessment.Reason}")],
            _ => [Warning(request, "STAGE-BASELINE-004", $"Release-baseline checking is unavailable: {assessment.Reason}")]
        };
    }

    static ArtifactRenderDiagnostic Violation(ArtifactRenderRequest request, StageReleaseBaselineViolation violation)
    {
        var changed = violation.Kind == "BaselinedRevisionChanged";
        var hint = string.Join('/', violation.DisplayHint.Parts.Select(part => part.Key));
        var difference = violation.Path is { } path ? $" First differing path: {path}." : string.Empty;

        return Warning(
            request,
            changed ? "STAGE-BASELINE-001" : "STAGE-BASELINE-002",
            $"Admitted event '{violation.StoredName}' ({hint}), contract {violation.ContractId}, revision {violation.Revision}, release '{violation.AdmittedIn}', is {(changed ? "changed" : "missing")}.{difference}");
    }

    static ArtifactRenderDiagnostic Warning(ArtifactRenderRequest request, string code, string message) =>
        new(code, ArtifactRenderDiagnosticSeverity.Warning, message, request.Model.Application.Id);
}
