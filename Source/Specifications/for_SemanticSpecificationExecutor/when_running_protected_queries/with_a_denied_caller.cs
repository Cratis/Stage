// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_protected_queries;

public class with_a_denied_caller : a_projected_specification
{
    void Establish() => _plan = Compile("""
        specification DenyingAnUnprivilegedReader
          given caller
            authenticated
          given InvoiceRegistered
            for "first"
            description = "North"
          when query InvoiceById
            invoiceId = "first"
          then denied
        """);

    Task Because() => Run();

    [Fact] void should_match_the_expected_denial() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_match_the_reference_execution() => _result.ExecutionKind.ShouldEqual(_reference.Execution.Kind.ToString());
    [Fact] void should_match_the_reference_expectations() => _reference.Passed.ShouldBeTrue();
    [Fact] void should_expose_no_query_data() => _result.Trace!.Queries.ShouldBeEmpty();
}
