// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_carrying_semantic_exclusions;

public class and_replacing_other_plan_evidence : given.a_request_with_exclusions
{
    ArtifactRenderPlan _combined = null!;

    void Because()
    {
        var warning = new ArtifactRenderDiagnostic(ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange, ArtifactRenderDiagnosticSeverity.Warning, "Input is newer than audited.", _request.Model.Application.Id);
        _combined = Create([warning]).WithPlanEvidence([new(warning, "Use an audited input pair.")]);
        _result = _combined.WithPlanEvidence([]);
    }

    [Fact] void should_keep_both_evidence_families() => _combined.PlanEvidence.Length.ShouldEqual(3);
    [Fact] void should_replace_ordinary_evidence() => _result.PlanEvidence.Length.ShouldEqual(2);
    [Fact] void should_keep_the_compiler_exclusions() => _result.Exclusions.Length.ShouldEqual(2);
}
