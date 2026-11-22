// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_running_sourceless_givens : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish()
    {
        var vector = canonical_corpus_vectors.All.Single(vector => vector.Name == "SourcelessGivensCorpus.V2");
        _plan = SemanticExecutionPlan.Compile(vector.Model, []).Plan!;
    }

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([.. _plan.Specifications.Keys]), new());

    [Fact] void should_report_every_specification() => _report.Results.Count.ShouldEqual(_plan.Specifications.Count);
    [Fact] void should_refuse_the_two_source_observing_projections() => Refused().Length.ShouldEqual(2);
    [Fact] void should_name_the_typed_code() => Assert.All(Refused(), result => Assert.StartsWith("STAGE-ESM-051:", result.Unsupported.Details));
    [Fact] void should_name_the_owning_issue() => Assert.All(Refused(), result => Assert.Contains("Cratis/Stage#960", result.Unsupported.Details));
    [Fact] void should_keep_the_other_specifications_supported() => Assert.All(_report.Results.Except(Refused()), result => Assert.Equal(SemanticSpecificationOutcome.Passed, result.Outcome));

    SemanticSpecificationRunRecord[] Refused() => [.. _report.Results.Where(result => result.Outcome == SemanticSpecificationOutcome.Unsupported)];
}
#endif
