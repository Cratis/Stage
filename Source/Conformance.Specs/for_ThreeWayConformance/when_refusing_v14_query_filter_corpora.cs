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

public class when_refusing_v14_query_filter_corpora : Specification
{
    [Fact]
    public async Task should_preserve_reference_outcomes_without_silently_ignoring_filters()
    {
        var vector = QueryFiltersCorpus.V14;
        var model = SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        foreach (var expectation in vector.SpecificationExpectations)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
            reference.Execution.Kind.ShouldEqual(expectation.Outcome);
            reference.Passed.ShouldBeTrue();
        }

        var stage = await new SemanticSpecificationExecutor().Run(plan, new([.. plan.Specifications.Keys]), new());
        stage.Results.ShouldNotBeEmpty();
        stage.Results.Select(result => result.Outcome).Distinct().ShouldContainOnly(SemanticSpecificationOutcome.Unsupported);
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("V14", "V14"));
        rendered.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-007");
        rendered.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-016");
        rendered.Success.ShouldBeFalse();
        rendered.Artifacts.ShouldBeEmpty();
    }
}
