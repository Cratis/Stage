// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_CratisEsmRangeAdmission;

public class when_scoping_security_findings : given.a_range_model
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_include_only_findings_in_selected_slices(bool selected)
    {
        Versions(32, 1, 32, 1);
        var (request, owner) = WithUnknownSecurity(selected);
        var verdict = CratisEsmRangeAdmission.Evaluate(request, new(request, new("Contracts", "Contracts")));
        verdict.IsAdmitted.ShouldEqual(!selected);
        verdict.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-016").ShouldEqual(selected ? 1 : 0);
        if (selected) verdict.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-016").Artifact.ShouldEqual(owner);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(34)]
    public void should_admit_invocation_expectations_in_the_range_verdict(uint major)
    {
        // The per-shape refusal (STAGE-ESM-016 on the specification) is made by SemanticSpecificationAdmission.ValidateInvocations, not by the range verdict.
        Versions(major, 0, major, 0);
        var captureSlice = _context.AllSlices.Single(located => !located.Slice.Captures.IsDefaultOrEmpty);
        var specification = captureSlice.Slice.Specifications[0];
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenInvocations)).SetValue(specification, System.Collections.Immutable.ImmutableArray.Create(new SemanticSpecificationInvocation(default, [])));
        var request = _request with { Scope = new(ArtifactRenderScopeKind.Slice, captureSlice.Slice.Id) };
        var verdict = CratisEsmRangeAdmission.Evaluate(request, new(request, new("Contracts", "Contracts")));
        verdict.IsAdmitted.ShouldBeTrue();
        verdict.Diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(34)]
    public void should_not_gate_other_rejected_members_in_audited_pairs(uint major)
    {
        var application = _model.Application with { Policies = [new("Sensitive policy", new SemanticAnonymousCondition())] };
        SetModel(ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application));
        Versions(major, 0, major, 0);
        var verdict = CratisEsmRangeAdmission.Evaluate(_request, _context);
        verdict.IsAdmitted.ShouldBeTrue();
        verdict.Diagnostics.ShouldBeEmpty();
    }

    (ArtifactRenderRequest Request, SemanticId Owner) WithUnknownSecurity(bool selected)
    {
        // An undefined slice kind is an unknown member the slice itself carries; a slice is not a finding owner, so the application receives it.
        var owning = _context.AllSlices.Single(located => !located.Slice.Captures.IsDefaultOrEmpty);
        typeof(SemanticSlice).GetProperty(nameof(SemanticSlice.Kind)).SetValue(owning.Slice, (SemanticSliceKind)9876);
        var slice = selected ? owning : _context.AllSlices.First(located => located.Slice.Id != owning.Slice.Id);
        return (_request with { Scope = new(ArtifactRenderScopeKind.Slice, slice.Slice.Id) }, _model.Application.Id);
    }

    [Fact]
    void should_fall_back_to_application_for_unowned_security_semantics()
    {
        var application = _model.Application with { Policies = [new("Sensitive policy", new SemanticAnonymousCondition())] };
        SetModel(ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application));
        Versions(32, 1, 32, 1);
        typeof(SemanticPolicy).GetProperty(nameof(SemanticPolicy.Condition)).SetValue(_model.Application.Policies.Single(), new an_unknown_security_condition());
        var verdict = CratisEsmRangeAdmission.Evaluate(_request, _context);
        verdict.IsAdmitted.ShouldBeFalse();
        var diagnostic = verdict.Diagnostics.Single(item => item.Code == "STAGE-ESM-016");
        diagnostic.Artifact.ShouldEqual(_model.Application.Id);
        diagnostic.Message.ShouldContain("an_unknown_security_condition.$type");
        diagnostic.Message.ShouldNotContain("Sensitive policy");
    }
}
