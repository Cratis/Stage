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

public class when_admitting_the_v15_golden_vector : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(15).Bytes.AsSpan());

    void Because()
    {
        var renderModel = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Name = "Golden" });
        _plan = CratisRendering.Plan(renderModel, SemanticExecutionPlan.Compile(renderModel, []).Plan!, new(ArtifactRenderScopeKind.Application, renderModel.Application.Id), new("Golden", "Golden"));
    }

    [Fact] void should_read_the_full_v15_vector() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V15);
    [Fact] void should_preserve_existing_construct_refusals() => _plan.Diagnostics.ShouldNotBeEmpty();
    [Fact] void should_not_refuse_the_audited_version() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-016");
    [Fact] void should_emit_no_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
