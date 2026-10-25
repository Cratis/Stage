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

    [Fact] void should_admit_the_v4_version_but_refuse_the_evolved_event() => rendering_refusals.Warning(_plan, "STAGE-ESM-026", EvolvedEvent().Id).Artifact.ShouldEqual(EvolvedEvent().Id);
    [Fact] void should_not_refuse_the_version() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
    [Fact] void should_emit_no_refused_event() => rendering_refusals.Omitted(_plan, EvolvedEvent().Id);
    [Fact] void should_record_partial_admission() => rendering_refusals.Partial(_plan);
    [Fact] void should_name_the_event_revision_and_missing_migrations() => rendering_refusals.Warning(
        _plan,
        "STAGE-ESM-026",
        EvolvedEvent().Id,
        "Event 'ProjectRegistered' has evolved to revision 2; Stage does not render event migrations yet.").Artifact.ShouldEqual(EvolvedEvent().Id);
#if DEBUG
    [Fact] void should_compile_the_retained_output() => rendering_refusals.Compiles(_plan);
#endif
    [Fact] void should_refuse_a_projection_dependency_outside_the_selected_slice()
    {
        var view = _v4.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.StateView);
        var plan = CratisRendering.Plan(_v4, SemanticExecutionPlan.Compile(_v4).Plan!, new(ArtifactRenderScopeKind.Slice, view.Id), _options);
        rendering_refusals.Warning(plan, "STAGE-ESM-026", EvolvedEvent().Id).Artifact.ShouldEqual(EvolvedEvent().Id);
        rendering_refusals.Omitted(plan, EvolvedEvent().Id);
        rendering_refusals.Omitted(plan, view.Id);
    }

    SemanticEventContract EvolvedEvent() => _v4.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events).Single(@event => @event.Revision != EventContractRevision.Initial);
}
