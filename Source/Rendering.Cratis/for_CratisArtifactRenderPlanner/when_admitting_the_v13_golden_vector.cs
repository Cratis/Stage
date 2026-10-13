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

public class when_admitting_the_v13_golden_vector : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        _model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(13).Bytes.AsSpan());
    }

    void Because()
    {
        var profile = CratisRendering.CreateProfile("Golden", new("Golden", "Golden"));
        _plan = new CratisArtifactRenderPlanner().Plan(new(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, profile, new(ArtifactRenderScopeKind.Application, _model.Application.Id)));
    }

    [Fact] void should_read_the_full_v13_vector() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V13);
    [Fact] void should_preserve_existing_construct_refusals() => _plan.Diagnostics.ShouldNotBeEmpty();
    [Fact] void should_not_refuse_the_audited_version() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-016");
    [Fact] void should_not_misclassify_valid_compliance() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-040");
    [Fact] void should_emit_no_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
