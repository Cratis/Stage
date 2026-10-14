// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_rejecting_invalid_query_arguments : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(QueryValidationCorpus.V18.EsmBytes.AsSpan())).Plan!;
    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_execute_every_corpus_specification() => _report.Results.Count.ShouldEqual(6);
    [Fact] void should_match_every_reference_expectation() => Assert.All(_report.Results, result => Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Name}: {result.Outcome}; {result.Unsupported?.Details}; {string.Join("; ", result.Failures)}"));
    [Fact] void should_classify_invalid_keys_and_filters_as_validation_rejections() => _report.Results.Where(result => result.Name.StartsWith("Invalid", StringComparison.Ordinal)).Select(result => result.Trace!.RejectionCategory).ShouldContainOnly("Validation", "Validation", "Validation", "Validation");
    [Fact] void should_preserve_the_authored_key_message() => _report.Results.Single(result => result.Name == "InvalidKey").Trace!.Rejection.ShouldEqual("Invoice number is too short");
    [Fact] void should_preserve_the_authored_filter_message() => _report.Results.Single(result => result.Name == "InvalidFilter").Trace!.Rejection.ShouldEqual("Customer code is too short");
    [Fact] void should_accept_valid_unmatched_keys_and_omitted_optional_filters() => _report.Results.Where(result => !result.Name.StartsWith("Invalid", StringComparison.Ordinal)).Select(result => result.ExecutionKind).ShouldContainOnly("Accepted", "Accepted");
}
