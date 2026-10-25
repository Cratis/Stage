// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_golden_destinationless_productions : Specification
{
    [Theory]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    [InlineData(21)]
    [InlineData(22)]
    [InlineData(23)]
    [InlineData(24)]
    public void should_report_only_the_exact_production_refusal(int version)
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(version).Bytes.AsSpan());
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model, []).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Golden", "Golden"));
        var diagnostics = plan.Diagnostics.Where(diagnostic => diagnostic.Artifact.ToString() == "sem1:000000000000000000000000000000000000000000000000000000000000277e").ToArray();
        diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-053");
        diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-006");
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
    }
}
