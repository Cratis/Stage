// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderOutcomeClassification;

public class when_classifying_generation : for_ArtifactImplementationGap.given.a_gap
{
    ArtifactRenderDiagnostic _warning = null!;

    void Establish() => _warning = new("STAGE-DEGRADE-002", ArtifactRenderDiagnosticSeverity.Warning, "Implementation described.", _request.Model.Application.Id);

    [Theory]
    [InlineData(ArtifactRenderOutcome.Rendered, ArtifactRenderOutcome.Degraded, ArtifactRenderOutcome.Degraded)]
    [InlineData(ArtifactRenderOutcome.Degraded, ArtifactRenderOutcome.Rendered, ArtifactRenderOutcome.Degraded)]
    [InlineData(ArtifactRenderOutcome.Refused, ArtifactRenderOutcome.Degraded, ArtifactRenderOutcome.Refused)]
    [InlineData(ArtifactRenderOutcome.Degraded, ArtifactRenderOutcome.Refused, ArtifactRenderOutcome.Refused)]
    public void should_aggregate_by_dominance(ArtifactRenderOutcome first, ArtifactRenderOutcome second, ArtifactRenderOutcome expected) =>
        ArtifactRenderOutcomeClassification.Aggregate([first, second]).ShouldEqual(expected);

    [Fact] public void should_render_an_empty_selection() => ArtifactRenderOutcomeClassification.Aggregate([]).ShouldEqual(ArtifactRenderOutcome.Rendered);
    [Fact] public void should_degrade_only_with_validated_evidence() => ArtifactRenderOutcomeClassification.Classify([_warning], [new(_warning, "Implement it.")]).ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] public void should_not_infer_evidence_from_a_code_prefix() => ArtifactRenderOutcomeClassification.Classify([_warning], []).ShouldEqual(ArtifactRenderOutcome.Rendered);
    [Fact] public void should_refuse_an_unrelated_error() => ArtifactRenderOutcomeClassification.Classify([_warning, _warning with { Code = "OTHER", Severity = ArtifactRenderDiagnosticSeverity.Error }], [new(_warning, "Implement it.")]).ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] public void should_not_treat_an_error_as_warning_evidence() => ArtifactRenderOutcomeClassification.Classify([_warning with { Severity = ArtifactRenderDiagnosticSeverity.Error }], []).ShouldEqual(ArtifactRenderOutcome.Refused);
    [Theory]
    [InlineData(ArtifactRenderOutcome.Rendered, ArtifactRenderOutcome.Degraded, ArtifactRenderOutcome.Refused)]
    [InlineData(ArtifactRenderOutcome.Rendered, ArtifactRenderOutcome.Refused, ArtifactRenderOutcome.Degraded)]
    [InlineData(ArtifactRenderOutcome.Degraded, ArtifactRenderOutcome.Rendered, ArtifactRenderOutcome.Refused)]
    [InlineData(ArtifactRenderOutcome.Degraded, ArtifactRenderOutcome.Refused, ArtifactRenderOutcome.Rendered)]
    [InlineData(ArtifactRenderOutcome.Refused, ArtifactRenderOutcome.Rendered, ArtifactRenderOutcome.Degraded)]
    [InlineData(ArtifactRenderOutcome.Refused, ArtifactRenderOutcome.Degraded, ArtifactRenderOutcome.Rendered)]
    public void should_refuse_every_mixed_permutation(ArtifactRenderOutcome first, ArtifactRenderOutcome second, ArtifactRenderOutcome third) =>
        ArtifactRenderOutcomeClassification.Aggregate([first, second, third]).ShouldEqual(ArtifactRenderOutcome.Refused);

    [Fact] public void should_refuse_when_an_unrelated_error_precedes_warning_evidence() => ArtifactRenderOutcomeClassification.Classify([_warning with { Code = "OTHER", Severity = ArtifactRenderDiagnosticSeverity.Error }, _warning], [new(_warning, "Implement it.")]).ShouldEqual(ArtifactRenderOutcome.Refused);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_reject_duplicate_evidence_before_aggregation(bool genuineError)
    {
        var diagnostics = genuineError ? new[] { _warning, _warning with { Code = "OTHER", Severity = ArtifactRenderDiagnosticSeverity.Error } } : [_warning];
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderOutcomeClassification.Classify([.. diagnostics], [new(_warning, "Implement it."), new(_warning, "Implement it.")]));
    }

    [Fact]
    public void should_classify_original_warnings_rather_than_strict_promotions()
    {
        var plan = ArtifactRenderPlan.Create(_request, [], [_warning]).WithAcceptance([new(_warning, "Implement it.")], strict: true);
        var original = plan.WithAcceptance(plan.Degradations);
        ArtifactRenderOutcomeClassification.Classify(original.Diagnostics, plan.Degradations).ShouldEqual(ArtifactRenderOutcome.Degraded);
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderOutcomeClassification.Classify(plan.Diagnostics, plan.Degradations));
    }

    [Fact] public void should_reject_evidence_with_a_different_message() => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderOutcomeClassification.Classify([_warning], [new(_warning with { Message = "Different." }, "Implement it.")]));
}
