// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// The canonical ESM v9 vector with a stateful capture condition must remain refused.
/// </summary>
/// <remarks>
/// The unmodified vector is supported; change tracking still requires persistent capture state.
/// </remarks>
public class when_refusing_the_canonical_public_events_corpus : given.a_translation_application
{
    void Establish() => _model = Transform(SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan()), slice => slice with
    {
        Captures = [.. slice.Captures.Select(capture => capture with
        {
            Appends = [.. capture.Appends.Select(append => append with { When = new(SemanticCaptureConditionKind.AnyChanged, [capture.Key]) })]
        })]
    });

    void Because() => _plan = CratisRendering.Plan(
        _model,
        SemanticExecutionPlan.Compile(_model).Plan!,
        new(ArtifactRenderScopeKind.Application, _model.Application.Id),
        new(_model.Application.Name, _model.Application.Name));

    [Fact] void should_load_an_esm_v9_model() => _model.SemanticVersion.Major.ShouldEqual(9u);
    [Fact] void should_carry_a_public_event() => _model.Application.Modules.SelectMany(Slices).SelectMany(slice => slice.Events).Any(@event => @event.Visibility == SemanticEventVisibility.Public).ShouldBeTrue();
    [Fact] void should_refuse_the_stateful_capture_condition() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-024" && diagnostic.Message.Contains("persistent capture state", StringComparison.Ordinal));
    [Fact] void should_have_no_reducer_bodies_to_analyze() => _model.Application.Modules.SelectMany(Slices).SelectMany(slice => slice.Reducers).ShouldBeEmpty();
    [Fact] void should_not_claim_reducer_transition_analysis() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-023");
    [Fact] void should_admit_the_version_pair() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-016");
    [Fact] void should_not_publish_a_partial_plan() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();

    static IEnumerable<SemanticSlice> Slices(SemanticModule module) => module.Features.SelectMany(Slices);

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => [.. feature.Slices, .. feature.Features.SelectMany(Slices)];
}
