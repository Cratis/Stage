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

public class when_refusing_the_canonical_v7_corpus : Specification
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
    [Fact] void should_refuse_generated_values_and_responses_only() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().Order(StringComparer.Ordinal).ShouldContainOnly(["STAGE-ESM-028", "STAGE-ESM-029"]);
    [Fact] void should_name_the_generating_command() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-028" && diagnostic.Message.StartsWith("Command 'RegisterProject' generates", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_name_the_command_response() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-029" && diagnostic.Message == "Command 'RegisterProject' declares a response (ESM v7), which Stage does not render yet.").ShouldBeTrue();
    [Fact] void should_not_publish_a_partial_plan() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
