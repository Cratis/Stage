// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_protected_queries;

public class with_an_unexpected_denial : a_projected_specification
{
    void Establish() => _plan = Compile("""
        specification DenialIsNotAnEmptyResult
          given caller
            authenticated
          when query InvoiceById
            invoiceId = "first"
          then no result
        """);

    Task Because() => Run();

    [Fact] void should_fail_instead_of_returning_an_empty_result() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_match_the_reference() => _reference.Passed.ShouldBeFalse();
    [Fact] void should_preserve_the_reference_failure() => _result.Failures.ShouldContainOnly(_reference.Failures);
}
