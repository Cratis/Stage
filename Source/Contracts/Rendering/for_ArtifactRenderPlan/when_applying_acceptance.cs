// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_applying_acceptance : given.an_artifact_render_request
{
    ArtifactRenderDiagnostic Warning => new("STAGE-DEGRADE-002", ArtifactRenderDiagnosticSeverity.Warning, "Implementation is unverified.", _request.Model.Application.Id);
    ArtifactRenderDegradation Gap => new(Warning, "Supply and verify the implementation.");
    ArtifactRenderPlan Plan => ArtifactRenderPlan.Create(_request, [PlannedArtifact.CreateText("app.cs", "application")], [Warning]);

    [Fact]
    void should_separate_default_acceptance_from_degraded_output()
    {
        var plan = Plan.WithAcceptance([Gap]);
        plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
        plan.Success.ShouldBeTrue();
        plan.CanPublishArtifacts.ShouldBeTrue();
        plan.Strict.ShouldBeFalse();
    }

    [Fact]
    void should_fail_strict_acceptance_without_discarding_safe_output()
    {
        var original = Plan;
        var strict = original.WithAcceptance([Gap], strict: true);
        strict.Success.ShouldBeFalse();
        strict.CanPublishArtifacts.ShouldBeTrue();
        strict.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
        strict.Diagnostics.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
        strict.Artifacts.SequenceEqual(original.Artifacts).ShouldBeTrue();
        strict.Degradations.Single().Diagnostic.Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
        original.Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData("STAGE-DEGRADE-002")]
    [InlineData("UNRELATED-ERROR")]
    void should_not_bless_an_unrelated_error_even_when_its_code_matches(string code)
    {
        var error = Warning with { Severity = ArtifactRenderDiagnosticSeverity.Error, Code = code };
        var plan = ArtifactRenderPlan.Create(_request, Plan.Artifacts, [Warning, error]).WithAcceptance([Gap], strict: true);
        plan.Success.ShouldBeFalse();
        plan.CanPublishArtifacts.ShouldBeFalse();
        plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
        plan.Diagnostics.Count(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldEqual(2);
    }

    [Fact]
    void should_not_promote_an_unrecorded_warning()
    {
        var other = Warning with { Message = "Another concern." };
        var plan = ArtifactRenderPlan.Create(_request, Plan.Artifacts, [Warning, other]).WithAcceptance([Gap], strict: true);
        plan.Diagnostics.Single(diagnostic => diagnostic.Message == other.Message).Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
    }

    [Fact]
    void should_restore_original_diagnostics_when_strict_acceptance_is_removed()
    {
        var plan = Plan.WithAcceptance([Gap], strict: true).WithAcceptance([Gap]);
        plan.Success.ShouldBeTrue();
        plan.CanPublishArtifacts.ShouldBeTrue();
        plan.Diagnostics.Single().ShouldEqual(Warning);
    }

    [Fact]
    void should_reject_uninitialized_metadata() => Catch.Exception(() => Plan.WithAcceptance(default)).ShouldBeOfExactType<InvalidArtifactRenderContract>();

    [Fact]
    void should_reject_duplicate_evidence() => Reject([Gap, Gap]);

    [Fact]
    void should_reject_missing_evidence() => Reject([Gap with { Diagnostic = Warning with { Message = "Not emitted." } }]);

    [Fact]
    void should_reject_original_errors_as_promotion_evidence() => Reject([Gap with { Diagnostic = Warning with { Severity = ArtifactRenderDiagnosticSeverity.Error } }]);

    [Fact]
    void should_reject_unrecognized_codes() => Reject([Gap with { Diagnostic = Warning with { Code = "STAGE-PRIVATE" } }]);

    [Fact]
    void should_require_an_actionable_remedy() => Reject([Gap with { Remedy = " " }]);

    [Fact]
    void should_require_a_semantic_identity() => Reject([Gap with { Diagnostic = Warning with { Artifact = default } }]);

    [Fact]
    void should_preserve_the_legacy_digest_without_metadata() => Plan.WithAcceptance([]).Digest.ShouldEqual(Plan.Digest);

    [Fact]
    void should_hash_strictness_and_remedies()
    {
        Plan.WithAcceptance([Gap]).Digest.ShouldNotEqual(Plan.WithAcceptance([Gap], strict: true).Digest);
        Plan.WithAcceptance([Gap]).Digest.ShouldNotEqual(Plan.WithAcceptance([Gap with { Remedy = "A different remedy." }]).Digest);
    }

    [Fact]
    void should_expose_canonical_names_and_numeric_compatibility()
    {
        ((int)ArtifactRenderOutcome.Rendered).ShouldEqual((int)ArtifactRenderOutcome.Complete);
        ((int)ArtifactRenderOutcome.Degraded).ShouldEqual((int)ArtifactRenderOutcome.Partial);
        ArtifactRenderOutcome.Complete.ToCanonicalName().ShouldEqual("Rendered");
        ArtifactRenderOutcome.Partial.ToCanonicalName().ShouldEqual("Degraded");
        ArtifactRenderOutcome.Refused.ToCanonicalName().ShouldEqual("Refused");
        Catch.Exception(() => ((ArtifactRenderOutcome)42).ToCanonicalName()).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    }

    void Reject(ImmutableArray<ArtifactRenderDegradation> gaps) => Catch.Exception(() => Plan.WithAcceptance(gaps, strict: true)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
