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
        var captureSlice = _context.AllSlices.Single(located => !located.Slice.Captures.IsDefaultOrEmpty);
        var specification = captureSlice.Slice.Specifications[0];
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenInvocations)).SetValue(specification, System.Collections.Immutable.ImmutableArray.Create(new SemanticSpecificationInvocation(default, [])));
        var slice = selected ? captureSlice : _context.AllSlices.First(located => located.Slice.Id != captureSlice.Slice.Id);
        var request = _request with { Scope = new(ArtifactRenderScopeKind.Slice, slice.Slice.Id) };
        var verdict = CratisEsmRangeAdmission.Evaluate(request, new(request, new("Contracts", "Contracts")));
        verdict.IsAdmitted.ShouldEqual(!selected);
        verdict.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-016").ShouldEqual(selected ? 1 : 0);
        if (selected) verdict.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-016").Artifact.ShouldEqual(specification.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_scope_invocation_expectations_in_the_audited_v34_pair(bool selected)
    {
        var (request, specification) = WithInvocationExpectation(34, selected);
        var verdict = CratisEsmRangeAdmission.Evaluate(request, new(request, new("Contracts", "Contracts")));
        verdict.IsAdmitted.ShouldEqual(!selected);
        verdict.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-016").ShouldEqual(selected ? 1 : 0);
        if (selected) verdict.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-016").Artifact.ShouldEqual(specification.Id);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(33)]
    public void should_not_scan_audited_pairs_before_v34(uint major)
    {
        var (request, _) = WithInvocationExpectation(major, true);
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

    (ArtifactRenderRequest Request, SemanticSpecification Specification) WithInvocationExpectation(uint major, bool selected)
    {
        Versions(major, 0, major, 0);
        var captureSlice = _context.AllSlices.Single(located => !located.Slice.Captures.IsDefaultOrEmpty);
        var specification = captureSlice.Slice.Specifications[0];
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenInvocations)).SetValue(specification, System.Collections.Immutable.ImmutableArray.Create(new SemanticSpecificationInvocation(default, [])));
        var slice = selected ? captureSlice : _context.AllSlices.First(located => located.Slice.Id != captureSlice.Slice.Id);
        return (_request with { Scope = new(ArtifactRenderScopeKind.Slice, slice.Slice.Id) }, specification);
    }

    [Fact]
    void should_fall_back_to_application_for_unowned_security_semantics()
    {
        var application = _model.Application with { Policies = [new("Sensitive policy", new SemanticAnonymousCondition())] };
        SetModel(ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application));
        Versions(32, 1, 32, 1);
        var verdict = CratisEsmRangeAdmission.Evaluate(_request, _context);
        verdict.IsAdmitted.ShouldBeFalse();
        var diagnostic = verdict.Diagnostics.Single(item => item.Code == "STAGE-ESM-016");
        diagnostic.Artifact.ShouldEqual(_model.Application.Id);
        diagnostic.Message.ShouldContain("SemanticAnonymousCondition.$type");
        diagnostic.Message.ShouldNotContain("Sensitive policy");
    }
}
