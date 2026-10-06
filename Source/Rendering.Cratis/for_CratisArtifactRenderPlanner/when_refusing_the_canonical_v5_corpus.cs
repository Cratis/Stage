// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_the_canonical_v5_corpus : Specification
{
    ArtifactRenderRequest _request = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(ReadModelAbsenceCorpus.V5.EsmBytes.AsSpan());
        _request = new(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            CratisRendering.CreateProfile(model.Application.Name, new("Absence", "Absence")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
    }

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    [Fact] void should_load_v5() => _request.Model.SemanticVersion.ShouldEqual(SemanticVersion.V5);
    [Fact] void should_keep_the_planner_version_gate_closed() => _plan.Diagnostics.Single().Code.ShouldEqual("STAGE-ESM-016");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_keep_the_semantic_admission_version_gate_closed()
    {
        var context = new SemanticApplicationContext(_request, new("Absence", "Absence"));
        SemanticCratisAdmission.Evaluate(context, context.SelectedSlices()).Single().Code.ShouldEqual("STAGE-ESM-016");
    }
}
