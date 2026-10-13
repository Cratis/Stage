// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_identity_only_every : a_universal_projection
{
    void Establish() => _plan = CreatePlan(false);

    Task Because() => Run();

    [Fact] void should_report_the_expected_outcome() => Assert.True(_result.Outcome == SemanticSpecificationOutcome.Passed, string.Join("; ", _result.Failures));
    [Fact] void should_match_the_reference() => Assert.True(_reference.Passed, string.Join("; ", _reference.Failures));
    [Fact] void should_trace_the_observed_instances() => _result.Trace!.ReadModels.Count.ShouldEqual(1);
}
