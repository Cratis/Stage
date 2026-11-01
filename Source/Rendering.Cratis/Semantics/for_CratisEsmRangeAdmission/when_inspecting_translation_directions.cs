// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_CratisEsmRangeAdmission;

public class when_inspecting_translation_directions : given.a_range_model
{
    [Theory]
    [InlineData(SemanticTranslationDirection.Inbound)]
    [InlineData(SemanticTranslationDirection.Outbound)]
    public void should_recognize_each_audited_direction_without_findings(SemanticTranslationDirection direction)
    {
        SetDirection(direction);
        var verdict = CratisEsmRangeAdmission.Evaluate(_request, _context);
        verdict.Standing.UnknownMembers.ShouldBeEmpty();
        verdict.IsAdmitted.ShouldBeTrue();
        verdict.Diagnostics.Single().Code.ShouldEqual(ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange);
    }

    [Fact]
    void should_refuse_an_undefined_direction_as_a_security_relevant_value()
    {
        SetDirection((SemanticTranslationDirection)int.MaxValue);
        var verdict = CratisEsmRangeAdmission.Evaluate(_request, _context);
        var member = verdict.Standing.UnknownMembers.Single();
        member.Member.ShouldEqual("SemanticTranslationDirection.$value");
        member.Path.ShouldEqual("Application.Modules[0].Features[0].Slices[1].Direction");
        member.SecurityRelevant.ShouldBeTrue();
        verdict.IsAdmitted.ShouldBeFalse();
        verdict.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-016").Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
    }

    void SetDirection(SemanticTranslationDirection direction)
    {
        var slice = _context.AllSlices.Single(located => !located.Slice.Captures.IsDefaultOrEmpty).Slice;
        typeof(SemanticSlice).GetProperty(nameof(SemanticSlice.Direction))!.SetValue(slice, direction);
        Versions(32, 1, 32, 1);
    }
}
