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
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_refusing_v10_corpora(ITestOutputHelper output) : Specification
{
    const string MappingFreeProjection = "A mapping-free from block on a read model without required properties does not materialize a Chronicle read model.";
    const string FollowupsProjection = "sem1:d4f86c078dd9e1b841b7393ad8412b82efb1c9db158fa3d03b78d8826e1d255a";

    [Theory]
    [InlineData("RoutedReaction", "DIVERGENCE #843")]
    [InlineData("UnroutedReaction", "DIVERGENCE #843")]
    [InlineData("WrongStreamReaction", "DIVERGENCE #843")]
    [InlineData("UnroutedReducer", "PARITY")]
    [InlineData("MatchingReducer", "PARITY")]
    public async Task should_report_each_canonical_outcome_independently(string name, string disposition)
    {
        var vector = EventRoutesCorpus.ObserverFiltersV10;
        var model = SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        plan.Specifications.Count.ShouldEqual(5);
        var expectation = vector.SpecificationExpectations.Single(expectation => expectation.Name == name);
        var reference = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
        reference.Execution.Kind.ShouldEqual(expectation.Outcome);
        if (expectation.Outcome != SemanticExecutionOutcomeKind.Unsupported) reference.Passed.ShouldBeTrue();
        var result = (await new SemanticSpecificationExecutor().Run(plan, new([expectation.Specification]), new())).Results.Single();
        if (disposition == "DIVERGENCE #843") AssertMappingFreeDivergence(reference, result);
        else AssertParity(result, expectation.Outcome);
        output.WriteLine($"{name}: {disposition}; Reference={reference.Execution.Kind}/Passed={reference.Passed}; Stage={result.Outcome}; {result.Unsupported}");
    }

    [Fact]
    public void should_preserve_complete_corpus_renderer_refusals()
    {
        var model = SemanticModelSerializer.Deserialize(EventRoutesCorpus.ObserverFiltersV10.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("V10", "V10"));
        rendered.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-011");
        rendered.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-016");
        rendered.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-035");
        rendered.Success.ShouldBeFalse();
        rendered.Artifacts.ShouldBeEmpty();
    }

    static void AssertMappingFreeDivergence(SemanticSpecificationRun reference, SemanticSpecificationRunRecord result)
    {
        reference.Execution.Kind.ShouldEqual(SemanticExecutionOutcomeKind.Accepted);
        reference.Passed.ShouldBeTrue();
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
        result.Unsupported.Construct.ShouldEqual(FollowupsProjection);
        result.Unsupported.Details.ShouldEqual(MappingFreeProjection);
    }

    static void AssertParity(SemanticSpecificationRunRecord result, SemanticExecutionOutcomeKind outcome)
    {
        var expected = outcome == SemanticExecutionOutcomeKind.Unsupported ? SemanticSpecificationOutcome.Unsupported : SemanticSpecificationOutcome.Passed;
        Assert.True(result.Outcome == expected, $"{result.Name}: expected {expected}, got {result.Outcome}; {result.Unsupported}");
        if (result.Unsupported is { } refusal)
        {
            refusal.Capability.ShouldEqual(StageExecutionCapability.Projection);
            refusal.Details.ShouldContain("STAGE-IMPL-003:");
        }
    }
}
