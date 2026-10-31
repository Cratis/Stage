// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_CratisEsmRangeAdmission;

public class when_evaluating_versions : given.a_range_model
{
    [Theory]
    [InlineData(25)]
    [InlineData(32)]
    public void should_preserve_audited_pairs(int version)
    {
        if (version == 25) SetModel(SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan()));
        var verdict = CratisEsmRangeAdmission.Evaluate(_request, _context);
        verdict.IsAdmitted.ShouldBeTrue();
        verdict.Standing.Version.ShouldEqual(EsmVersionStanding.Audited);
        verdict.Diagnostics.ShouldBeEmpty();
        verdict.Evidence.ShouldBeEmpty();
    }

    [Fact]
    void should_admit_a_clean_newer_minor_with_one_matching_warning()
    {
        Versions(32, 1, 32, 1);
        var verdict = CratisEsmRangeAdmission.Evaluate(_request, _context);
        verdict.Standing.UnknownMembers.ShouldBeEmpty();
        verdict.IsAdmitted.ShouldBeTrue();
        verdict.Diagnostics.Single().Code.ShouldEqual(ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange);
        verdict.Diagnostics.Single().Artifact.ShouldEqual(_model.Application.Id);
        verdict.Diagnostics.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
        verdict.Evidence.Single().Diagnostic.ShouldEqual(verdict.Diagnostics.Single());
        verdict.Evidence.Single().Remedy.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData(33, 1, 33, 1)]
    [InlineData(34, 1, 34, 1)]
    [InlineData(35, 0, 35, 0)]
    [InlineData(31, 0, 31, 0)]
    [InlineData(32, 0, 32, 1)]
    public void should_refuse_non_admitted_pairs(uint languageMajor, uint languageMinor, uint semanticMajor, uint semanticMinor)
    {
        Versions(languageMajor, languageMinor, semanticMajor, semanticMinor);
        var verdict = CratisEsmRangeAdmission.Evaluate(_request, _context);
        verdict.IsAdmitted.ShouldBeFalse();
        verdict.Diagnostics.Single().Code.ShouldEqual("STAGE-ESM-016");
        verdict.Diagnostics.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
        verdict.Diagnostics.Single().Message.ShouldContain(">=32.0 <33");
        verdict.Evidence.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_keep_unknown_security_fail_closed_and_explicit_non_security_evidence(bool security)
    {
        Versions(32, 1, 32, 1);
        var verdict = CratisEsmRangeAdmission.FromFindings(_request, new(EsmVersionStanding.NewerWithinRange,
            [new(_model.Application.Id, "application", "SemanticApplication.Future", "Application.Future", security)]));
        verdict.IsAdmitted.ShouldEqual(!security);
        verdict.Diagnostics.Length.ShouldEqual(2);
        verdict.Diagnostics[1].Code.ShouldEqual(security ? "STAGE-ESM-016" : ArtifactRenderDegradationCodes.UnknownOwnerMember);
        verdict.Evidence.Length.ShouldEqual(security ? 1 : 2);
        if (!security) verdict.Evidence[1].Diagnostic.ShouldEqual(verdict.Diagnostics[1]);
    }
}
