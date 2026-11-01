// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_validating_degradation_codes : given.an_artifact_render_request
{
    [Theory]
    [InlineData("STAGE-DEGRADE-001", "Unknown owner member.")]
    [InlineData("STAGE-DEGRADE-002", "Unsupported construct.")]
    [InlineData("STAGE-DEGRADE-003", "Inline language or placement mismatch.")]
    [InlineData("STAGE-DEGRADE-004", "Newer ESM within a declared range.")]
    [InlineData("STAGE-DEGRADE-005", "Dependency degradation or omission.")]
    void should_accept_only_exact_public_codes_as_original_warning_evidence(string code, string intent)
    {
        var warning = Warning(code, intent);
        var original = ArtifactRenderPlan.Create(_request, [], [warning]);
        var degraded = original.WithAcceptance([new(warning, "Resolve this modeled requirement.")]);
        var strict = degraded.WithAcceptance(degraded.Degradations, strict: true);
        degraded.Degradations.Single().Diagnostic.Code.ShouldEqual(code);
        degraded.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
        degraded.Success.ShouldBeTrue();
        strict.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
        strict.Success.ShouldBeFalse();
        strict.CanPublishArtifacts.ShouldBeTrue();
        strict.Degradations.Single().Diagnostic.ShouldEqual(warning);
        strict.Diagnostics.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("STAGE-UNSUPPORTED")]
    [InlineData("STAGE-DEFERRED")]
    [InlineData("STAGE-INCOMPLETE")]
    [InlineData("STAGE-DESCRIBED")]
    [InlineData("STAGE-UNVERIFIED")]
    [InlineData("STAGE-DEGRADE-000")]
    [InlineData("STAGE-DEGRADE-006")]
    [InlineData("STAGE-DEGRADE-002-extra")]
    [InlineData("stage-degrade-002")]
    [InlineData("STAGE-IMPL-006")]
    [InlineData("STAGE-PRIVATE")]
    void should_reject_ad_hoc_supporting_and_unknown_codes_even_when_the_warning_was_emitted(string code)
    {
        var warning = Warning(code, "An emitted warning is insufficient without a public degradation code.");
        var plan = ArtifactRenderPlan.Create(_request, [], [warning]);
        Catch.Exception(() => plan.WithAcceptance([new(warning, "Resolve the requirement.")], strict: true)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    }

    ArtifactRenderDiagnostic Warning(string code, string message) => new(code, ArtifactRenderDiagnosticSeverity.Warning, message, _request.Model.Application.Id);
}
