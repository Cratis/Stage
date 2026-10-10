// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_reporting_case_failures;

public class with_a_passing_case : a_case_table
{
    async Task Because() => _result = (await new SemanticSpecificationExecutor().Run(_loaded.Plan, new([]), new() { SpecificationOrigins = _loaded.SpecificationOrigins })).Results.Single(result => result.Name == "Registering_Passing");

    [Fact] void should_pass_the_derived_specification() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_leave_failures_empty() => _result.Failures.ShouldBeEmpty();
    [Fact] void should_retain_the_rejected_execution() => _result.ExecutionKind.ShouldEqual("Rejected");
}
