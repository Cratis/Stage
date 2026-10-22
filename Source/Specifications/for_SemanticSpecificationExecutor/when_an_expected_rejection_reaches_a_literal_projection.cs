// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_an_expected_rejection_reaches_a_literal_projection : given.a_universal_projection
{
    void Establish() => _plan = CreatePlan(false, mapping: "\"fixed\"", expectRejection: true);

    async Task Because() => await Run();

    [Fact] void should_report_an_outcome_mismatch() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_preserve_the_mismatch_message() => _result.Failures.ShouldContainOnly("Expected Rejected, got Accepted.");
    [Fact] void should_match_the_reference() => _result.Failures.ShouldContainOnly(_reference.Failures);
    [Fact] void should_not_evaluate_the_projection() => _result.Trace!.ReadModels.ShouldBeEmpty();
    [Fact] void should_not_report_a_rejection() => _result.Trace!.Rejection.ShouldBeNull();
}
