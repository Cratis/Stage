// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_keeping_safety_refusals;

public class with_policy_conflicts_outside_the_selected_scope : Specification
{
    ExecutableSemanticModel _model = null!;
    SemanticFeature _feature = null!;
    ArtifactRenderPlan _slicePlan = null!;
    ArtifactRenderPlan _featurePlan = null!;

    void Establish()
    {
        _model = Semantics.Policies.for_SemanticPolicyNames.when_policy_bindings_conflict.CompileModel();
        _feature = _model.Application.Modules.Single().Features.Single(feature => feature.Name == "Unprotected");
    }

    void Because()
    {
        _slicePlan = invoice_model.Plan(_model, new(ArtifactRenderScopeKind.Slice, _feature.Slices.Single().Id));
        _featurePlan = invoice_model.Plan(_model, new(ArtifactRenderScopeKind.Feature, _feature.Id));
    }

    [Fact] void should_refuse_the_slice_scope_without_throwing() => _slicePlan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_refuse_the_feature_scope_without_throwing() => _featurePlan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_report_the_authorization_conflict_in_the_slice_scope() => _slicePlan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-AUTH-002");
    [Fact] void should_report_the_authorization_conflict_in_the_feature_scope() => _featurePlan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-AUTH-002");
    [Fact] void should_emit_no_slice_artifacts() => _slicePlan.Artifacts.ShouldBeEmpty();
    [Fact] void should_emit_no_feature_artifacts() => _featurePlan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_render_around_the_conflict_in_the_slice_scope() => _slicePlan.Refusals.ShouldBeEmpty();
    [Fact] void should_not_render_around_the_conflict_in_the_feature_scope() => _featurePlan.Refusals.ShouldBeEmpty();
}
#endif
