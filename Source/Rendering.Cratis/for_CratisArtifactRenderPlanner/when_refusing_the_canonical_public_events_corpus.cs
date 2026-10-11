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
/// The canonical ESM v9 vector - public events, event origin, translation direction, event-target projections and
/// reducers, and events-source captures - planned by Stage.
/// </summary>
/// <remarks>
/// The audited v9 version is admitted; each unsupported construct refuses the plan before anything is emitted.
/// </remarks>
public class when_refusing_the_canonical_public_events_corpus : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());

    void Because() => _plan = CratisRendering.Plan(
        _model,
        SemanticExecutionPlan.Compile(_model).Plan!,
        new(ArtifactRenderScopeKind.Application, _model.Application.Id),
        new(_model.Application.Name, _model.Application.Name));

    [Fact] void should_load_an_esm_v9_model() => _model.SemanticVersion.Major.ShouldEqual(9u);
    [Fact] void should_carry_a_public_event() => _model.Application.Modules.SelectMany(Slices).SelectMany(slice => slice.Events).Any(@event => @event.Visibility == SemanticEventVisibility.Public).ShouldBeTrue();
    [Fact] void should_refuse_each_kind_of_construct() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-024", "STAGE-ESM-031", "STAGE-ESM-032"]);
    [Fact] void should_admit_the_version_pair() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-016");
    [Fact] void should_not_publish_a_partial_plan() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();

    static IEnumerable<SemanticSlice> Slices(SemanticModule module) => module.Features.SelectMany(Slices);

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => [.. feature.Slices, .. feature.Features.SelectMany(Slices)];
}
