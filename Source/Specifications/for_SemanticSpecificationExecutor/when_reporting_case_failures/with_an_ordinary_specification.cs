// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_reporting_case_failures;

public class with_an_ordinary_specification : a_case_table
{
    SemanticSpecificationRunRecord _withoutOrigins = null!;

    async Task Because()
    {
        var executor = new SemanticSpecificationExecutor();
        _result = (await executor.Run(_loaded.Plan, new([]), new() { SpecificationOrigins = _loaded.SpecificationOrigins })).Results.Single(result => result.Name == "Ordinary");
        _withoutOrigins = (await executor.Run(_loaded.Plan, new([]), new())).Results.Single(result => result.Name == "Ordinary");
    }

    [Fact] void should_retain_the_failed_outcome() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_leave_the_failure_text_unchanged() => _result.Failures.ShouldContainOnly(_withoutOrigins.Failures);
}
