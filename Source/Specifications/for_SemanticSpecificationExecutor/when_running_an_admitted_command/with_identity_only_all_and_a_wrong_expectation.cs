// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_identity_only_all_and_a_wrong_expectation : a_universal_projection
{
    void Establish() => _plan = CreatePlan(true, "wrong");

    Task Because() => Run();

    [Fact] void should_report_the_expected_outcome() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_match_the_reference() => _reference.Passed.ShouldBeFalse();
    [Fact] void should_trace_the_observed_instances() => _result.Trace!.ReadModels.Count.ShouldEqual(2);
}
