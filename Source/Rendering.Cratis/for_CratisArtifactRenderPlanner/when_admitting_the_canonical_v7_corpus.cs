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

public class when_admitting_the_canonical_v7_corpus : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _model = SemanticModelSerializer.Deserialize(RegisterProjectCorpus.V7.EsmBytes.AsSpan());

    void Because() => _plan = CratisRendering.Plan(
        _model,
        SemanticExecutionPlan.Compile(_model).Plan!,
        new(ArtifactRenderScopeKind.Application, _model.Application.Id),
        new("Projects", "Projects"));

    [Fact] void should_load_an_esm_v7_model() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
    [Fact] void should_admit_generated_values_and_responses() => _plan.Success.ShouldBeTrue();
    [Fact] void should_report_no_refusals() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_emit_artifacts() => _plan.Artifacts.ShouldNotBeEmpty();
}
