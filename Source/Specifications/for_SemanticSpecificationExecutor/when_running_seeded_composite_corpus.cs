// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_seeded_composite_corpus : Specification
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRunRecord[] _seeded = [];

    async Task Because()
    {
        var model = SemanticModelSerializer.Deserialize(CompositeReadModelKeysCorpus.V27.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        _report = await new SemanticSpecificationExecutor().Run(plan, new([]), new());
        _seeded = [.. _report.Results.Where(result => !plan.Specifications[SemanticId.Parse(result.SpecificationId)].GivenReadModels.IsEmpty)];
    }

    [Fact] void should_cover_seeded_worlds() => _seeded.ShouldNotBeEmpty();
    [Fact] void should_pass_every_seeded_composite_world() => Assert.All(_seeded.Where(result => result.Name != "ScalarListBy"), result => Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Name}: {result.Outcome}; {result.Unsupported?.Details}; {string.Join(';', result.Failures)}"));
    [Fact] void should_filter_on_a_seeded_key_part() => _report.Results.Single(result => result.Name == "FilterOnePart").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact]
    void should_preserve_scalar_list_by_refusal()
    {
        var result = _report.Results.Single(result => result.Name == "ScalarListBy");
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported.Capability.ShouldEqual(StageExecutionCapability.Query);
        result.Unsupported.Details.ShouldEqual("Only optional snapshot queries by read-model identifier are admitted.");
    }

    [Fact] void should_remove_only_matching_parts() => _report.Results.Single(result => result.Name == "RemoveOnlyMatchingParts").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
