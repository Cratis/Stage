// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_CratisEsmRangeAdmission;

public class when_planning_with_range_admission : given.a_range_model
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_render_a_clean_newer_minor_as_degraded_even_when_strict(bool strict)
    {
        var feature = UseCaptureOnly();
        Versions(32, 1, 32, 1);
        var options = new CratisRenderingOptions("Contracts", "Contracts") { Strict = strict };
        var request = _request with
        {
            Profile = CratisRendering.CreateProfile(_model.Application.Name, options),
            Scope = new(ArtifactRenderScopeKind.Feature, feature.Id)
        };
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        plan.CanPublishArtifacts.ShouldBeTrue();
        plan.Success.ShouldEqual(!strict);
        plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error)
            .All(diagnostic => diagnostic.Code == ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange).ShouldBeTrue();
        plan.PlanEvidence.Single().Diagnostic.Code.ShouldEqual(ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange);
        plan.Degradations.ShouldBeEmpty();
        plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
        plan.Diagnostics.Count(diagnostic => diagnostic.Code == ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange).ShouldEqual(1);
        plan.Artifacts.ShouldNotBeEmpty();
    }

    [Fact]
    void should_admit_the_newer_minor_in_standalone_structural_admission()
    {
        UseCaptureOnly();
        Versions(32, 1, 32, 1);
        SemanticCratisAdmission.Evaluate(_context, _context.SelectedSlices()).ShouldNotContain(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData(34)]
    public void should_refuse_future_majors(uint major)
    {
        Versions(major, 0, major, 0);
        var plan = new CratisArtifactRenderPlanner().Plan(_request);
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
        plan.Artifacts.ShouldBeEmpty();
    }

    [Fact]
    void should_refuse_a_v33_minor_that_is_not_the_exact_audited_pair()
    {
        Versions(33, 1, 33, 1);
        var plan = new CratisArtifactRenderPlanner().Plan(_request);
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
        plan.Artifacts.ShouldBeEmpty();
    }

    [Fact]
    void should_refuse_unknown_security_before_rendering()
    {
        var application = _model.Application with { Policies = [new("Private policy", new SemanticAnonymousCondition())] };
        SetModel(ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application));
        Versions(32, 1, 32, 1);
        var plan = new CratisArtifactRenderPlanner().Plan(_request);
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-016" && diagnostic.Message.Contains("SemanticAnonymousCondition.$type", StringComparison.Ordinal));
    }
}
