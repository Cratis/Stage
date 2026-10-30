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
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenInvocations))!.SetValue(specification, System.Collections.Immutable.ImmutableArray.Create(new SemanticSpecificationInvocation(default, [])));
        var slice = selected ? captureSlice : _context.AllSlices.First(located => located.Slice.Id != captureSlice.Slice.Id);
        var request = _request with { Scope = new(ArtifactRenderScopeKind.Slice, slice.Slice.Id) };
        var verdict = CratisEsmRangeAdmission.Evaluate(request, new(request, new("Contracts", "Contracts")));
        verdict.IsAdmitted.ShouldEqual(!selected);
        verdict.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-016").ShouldEqual(selected ? 1 : 0);
        if (selected) verdict.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-016").Artifact.ShouldEqual(specification.Id);
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
