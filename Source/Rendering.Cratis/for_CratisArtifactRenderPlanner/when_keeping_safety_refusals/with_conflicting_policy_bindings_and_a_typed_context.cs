// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_keeping_safety_refusals;

public class with_conflicting_policy_bindings_and_a_typed_context : Specification
{
    LoadedSemanticModel _loaded = null!;
    ArtifactRenderPlan _plan = null!;

    async Task Establish()
    {
        var reducer = when_rendering_a_pure_reducer.WithIdentifierConcept(when_rendering_a_pure_reducer.Source)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal);
        _loaded = await when_rendering_a_pure_reducer.Load(Semantics.Policies.for_SemanticPolicyNames.when_policy_bindings_conflict.Source() + "\n" + reducer);
    }

    void Because() => _plan = when_rendering_a_pure_reducer.Plan(_loaded);

    [Fact] void should_exercise_a_reducer_typed_context() => _loaded.TypedContextDescriptors.Single().Role.ShouldEqual(SemanticImplementationRole.ReducerTransition);
    [Fact] void should_return_a_refused_plan_without_throwing() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_admit_the_typed_context_into_the_render_loop() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-021");
    [Fact] void should_refuse_only_for_the_authorization_conflict() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly("STAGE-AUTH-002");
    [Fact] void should_report_the_authorization_conflict() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-AUTH-002");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_render_around_the_conflict() => _plan.Refusals.ShouldBeEmpty();
}
#endif
