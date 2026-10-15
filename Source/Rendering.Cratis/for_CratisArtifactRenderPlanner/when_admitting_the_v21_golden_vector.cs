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

public class when_admitting_the_v21_golden_vector : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(21).Bytes.AsSpan());
    void Because()
    {
        var model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Name = "Golden" });
        _plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model, []).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Golden", "Golden"));
    }

    [Fact] void should_read_the_full_v21_vector() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
    [Fact] void should_not_refuse_the_audited_version() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-016");
    [Fact] void should_emit_no_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Theory]
    [InlineData("PatternReference", "pattern")]
    [InlineData("TicketNumber", "random")]
    [InlineData("TenantOrderId", "rule")]
    void should_name_each_unsupported_strategy(string name, string strategy) => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-028" && diagnostic.Artifact == _model.Application.Concepts.Single(concept => concept.Name == name).Id && diagnostic.Message.Contains(strategy, StringComparison.Ordinal));
    [Fact] void should_refuse_checksum_validation() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-042" && diagnostic.Message.Contains("checksum", StringComparison.Ordinal));
}
