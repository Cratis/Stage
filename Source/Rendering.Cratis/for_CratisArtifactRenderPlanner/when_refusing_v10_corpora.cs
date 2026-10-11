// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_v10_corpora : Specification
{
    [Theory]
    [InlineData("identity", "STAGE-ESM-034")]
    [InlineData("filters", "STAGE-ESM-035")]
    [InlineData("production", "STAGE-ESM-036")]
    public void should_admit_the_version_but_refuse_the_construct_without_artifacts(string form, string code)
    {
        var vector = form switch
        {
            "identity" => ReactionIdentityCorpus.V10,
            "filters" => EventRoutesCorpus.ObserverFiltersV10,
            _ => EventRoutesCorpus.ProductionRoutesV10
        };
        var model = SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan());
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("V10", "V10"));
        model.SemanticVersion.Major.ShouldEqual(10u);
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-016");
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        if (form == "identity") plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-024", "STAGE-ESM-034"]);
    }
}
