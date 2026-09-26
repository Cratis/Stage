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

// Screenplay v4.37 evolved RegisterProject/V2's event into two generations; the corpus now selects ESM v4.
public class when_refusing_the_canonical_v4_corpus : a_register_project_render_request
{
    ExecutableSemanticModel _v4 = null!;
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        _v4 = SemanticModelSerializer.Deserialize(RegisterProjectCorpus.V2.EsmBytes.AsSpan());
        _plan = CratisRendering.Plan(
            _v4,
            SemanticExecutionPlan.Compile(_v4).Plan!,
            new(ArtifactRenderScopeKind.Application, _v4.Application.Id),
            _options);
    }

    [Fact] void should_load_an_esm_v4_model() => _v4.SemanticVersion.ShouldEqual(SemanticVersion.V4);
    [Fact] void should_refuse_generation_replay_before_v4_admission()
    {
        _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
        _plan.Artifacts.ShouldBeEmpty();
    }
}
