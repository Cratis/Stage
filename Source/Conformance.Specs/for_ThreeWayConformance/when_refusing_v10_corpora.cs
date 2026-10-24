// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_refusing_v10_corpora : Specification
{
    [Theory]
    [InlineData("filters", "STAGE-ESM-011")]
    public async Task should_preserve_reference_outcomes_and_renderer_refusals(string form, string code)
    {
        var vector = form switch
        {
            "filters" => EventRoutesCorpus.ObserverFiltersV10,
            _ => EventRoutesCorpus.ProductionRoutesV10
        };
        var model = SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        foreach (var expectation in vector.SpecificationExpectations)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
            reference.Execution.Kind.ShouldEqual(expectation.Outcome);
            if (expectation.Outcome != SemanticExecutionOutcomeKind.Unsupported) reference.Passed.ShouldBeTrue();
        }
        var stage = await new SemanticSpecificationExecutor().Run(plan, new([.. plan.Specifications.Keys]), new(), TestContext.Current.CancellationToken);
        stage.Results.Select(result => result.Outcome).Distinct().ShouldContainOnly([SemanticSpecificationOutcome.Unsupported]);
        stage.Results.All(result => result.Unsupported.Details.StartsWith((form == "filters" ? "STAGE-ESM-035" : code) + ":", StringComparison.Ordinal)).ShouldBeTrue();
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("V10", "V10"));
        rendered.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
        rendered.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-016");
        if (form == "filters") rendered.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-035");
        rendered.Success.ShouldBeFalse();
        rendered.Artifacts.ShouldBeEmpty();
    }
}
