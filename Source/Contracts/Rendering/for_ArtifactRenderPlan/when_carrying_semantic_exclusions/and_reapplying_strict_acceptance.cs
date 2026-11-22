// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_carrying_semantic_exclusions;

public class and_reapplying_strict_acceptance : given.a_request_with_exclusions
{
    void Because() => _result = Create([new("OTHER001", ArtifactRenderDiagnosticSeverity.Warning, "Unrelated warning.", _request.Model.Application.Id)])
        .WithInventory(ArtifactRenderInventory.Create([])).WithPlanEvidence([]).WithAcceptance([], strict: true)
        .WithInlineCodeFingerprint(new string('a', 64)).WithInputComponents([]).WithAcceptance([], strict: true);

    [Fact] void should_fail_acceptance() => _result.Success.ShouldBeFalse();
    [Fact] void should_keep_safe_artifacts_publishable() => _result.CanPublishArtifacts.ShouldBeTrue();
    [Fact] void should_preserve_exclusions() => _result.Exclusions.Length.ShouldEqual(2);
    [Fact] void should_preserve_fixed_evidence() => _result.PlanEvidence.Length.ShouldEqual(2);
    [Fact] void should_promote_only_the_exclusion_warnings() => _result.Diagnostics.Count(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldEqual(2);
    [Fact] void should_leave_unrelated_warnings_alone() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "OTHER001").Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
    [Fact] void should_restore_original_warnings() => _result.WithAcceptance([]).Diagnostics.All(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_not_score_excluded_leaves() => _result.Inventory!.Constructs.IsEmpty.ShouldBeTrue();
}
