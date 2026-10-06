// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_the_canonical_v4_corpus : a_register_project_render_request
{
    ExecutableSemanticModel _v4 = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _v4 = SemanticModelSerializer.Deserialize(RegisterProjectCorpus.V2.EsmBytes.AsSpan());

    void Because() => _plan = CratisRendering.Plan(
        _v4,
        SemanticExecutionPlan.Compile(_v4).Plan!,
        new(ArtifactRenderScopeKind.Application, _v4.Application.Id),
        _options);

    [Fact] void should_admit_the_v4_version_but_refuse_the_evolved_event() => _plan.Diagnostics.Single().Code.ShouldEqual("STAGE-ESM-026");
    [Fact] void should_emit_no_partial_application() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_fail_admission() => _plan.Success.ShouldBeFalse();
    [Fact] void should_name_the_event_revision_and_missing_migrations() => _plan.Diagnostics.Single().Message.ShouldEqual("Event 'ProjectRegistered' has evolved to revision 2; Stage does not render event migrations yet.");
    [Fact] void should_refuse_a_projection_dependency_outside_the_selected_slice()
    {
        var view = _v4.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.StateView);
        var plan = CratisRendering.Plan(_v4, SemanticExecutionPlan.Compile(_v4).Plan!, new(ArtifactRenderScopeKind.Slice, view.Id), _options);
        plan.Diagnostics.Single().Code.ShouldEqual("STAGE-ESM-026");
        plan.Artifacts.ShouldBeEmpty();
    }
}
