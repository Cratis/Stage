// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_keeping_safety_refusals;

public class with_conflicting_policy_bindings : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _model = Semantics.Policies.for_SemanticPolicyNames.when_policy_bindings_conflict.CompileModel();

    void Because() => _plan = invoice_model.Plan(_model);

    [Fact] void should_return_a_refused_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_report_the_authorization_conflict() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ConflictingPolicyBindings.Code);
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_render_around_the_conflict() => _plan.Refusals.ShouldBeEmpty();
}
#endif
