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

// The released ESM v2 vector: a typed destination and a specification that states the stream it expects.
public class when_planning_the_canonical_v2_corpus : a_register_project_render_request
{
    ExecutableSemanticModel _v2 = null!;
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        _v2 = SemanticModelSerializer.Deserialize(RegisterProjectCorpus.V2.EsmBytes.AsSpan());
        _plan = CratisRendering.Plan(
            _v2,
            SemanticExecutionPlan.Compile(_v2).Plan!,
            new(ArtifactRenderScopeKind.Application, _v2.Application.Id),
            _options);
    }

    [Fact] void should_load_an_esm_v2_model() => _v2.SemanticVersion.ShouldEqual(SemanticVersion.V2);
    [Fact] void should_plan_the_application() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_assert_the_event_on_the_stated_stream() =>
        Text(_plan.Artifacts.Single(_ => _.RelativePath.EndsWith("when_registering_aproject.cs", StringComparison.Ordinal)))
            .ShouldContain("ShouldHaveAppendedEvent<RegisterProject, ProjectRegistered>(new ProjectId(Guid.Parse(\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"))");
}
