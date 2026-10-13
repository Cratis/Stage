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

public class when_admitting_the_v14_golden_vector : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(14).Bytes.AsSpan());

    void Because()
    {
        // Golden vectors deliberately use a display name; the scaffold requires a C# application name.
        var renderModel = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Name = "Golden" });
        _plan = new CratisArtifactRenderPlanner().Plan(new(renderModel, SemanticExecutionPlan.Compile(renderModel, []).Plan!, CratisRendering.CreateProfile("Golden", new("Golden", "Golden")), new(ArtifactRenderScopeKind.Application, renderModel.Application.Id)));
    }

    [Fact] void should_read_the_full_v14_vector() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V14);
    [Fact] void should_preserve_existing_construct_refusals() => _plan.Diagnostics.ShouldNotBeEmpty();
    [Fact] void should_reach_semantic_admission() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-CRATIS-001");
    [Fact] void should_not_refuse_the_audited_version() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-016");
    [Fact] void should_emit_no_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
