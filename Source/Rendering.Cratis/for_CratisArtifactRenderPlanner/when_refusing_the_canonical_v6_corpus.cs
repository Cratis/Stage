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

public class when_refusing_the_canonical_v6_corpus : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderRequest _request = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        _model = SemanticModelSerializer.Deserialize(ReactionsCorpus.V6.EsmBytes.AsSpan());
        _request = new(
            _model,
            SemanticExecutionPlan.Compile(_model).Plan!,
            CratisRendering.CreateProfile(_model.Application.Name, new("Reactions", "Reactions")),
            new(ArtifactRenderScopeKind.Application, _model.Application.Id));
    }

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    [Fact] void should_load_an_esm_v6_model() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_keep_the_planner_version_gate_closed() => _plan.Diagnostics.Single().Code.ShouldEqual("STAGE-ESM-016");
    [Fact] void should_not_publish_a_partial_plan() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_keep_the_semantic_admission_version_gate_closed()
    {
        var context = new SemanticApplicationContext(_request, new("Reactions", "Reactions"));
        SemanticCratisAdmission.Evaluate(context, context.SelectedSlices()).Single().Code.ShouldEqual("STAGE-ESM-016");
    }
    [Fact] void should_classify_every_v6_member_as_rejected()
    {
        var entries = SemanticSurfaceLedger.Entries.Values.Where(entry => entry.Detail == "STAGE-ESM-024").ToArray();
        entries.Length.ShouldEqual(100);
        entries.All(entry => entry.Kind == SemanticSurfaceDispositionKind.Rejected).ShouldBeTrue();
    }
}
