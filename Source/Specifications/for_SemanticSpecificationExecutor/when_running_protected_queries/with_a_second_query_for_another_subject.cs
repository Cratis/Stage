// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_protected_queries;

public class with_a_second_query_for_another_subject : a_projected_specification
{
    void Establish() => _plan = Compile(
        """
        specification EachQueryChecksItsOwnSubject
          given caller
            authenticated
            claim "owner" = "first"
          then query InvoiceById
            arguments
              invoiceId = "first"
          then query InvoiceById
            arguments
              invoiceId = "second"
        """,
        "require claim \"owner\" matches subject");

    Task Because() => Run();

    [Fact] void should_not_reuse_the_first_queries_authorization() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_match_the_reference() => _reference.Passed.ShouldBeFalse();
    [Fact] void should_match_the_reference_denial() => _result.ExecutionKind.ShouldEqual(_reference.Execution.Kind.ToString());
    [Fact] void should_not_expose_the_first_queries_data() => _result.Trace!.Queries.ShouldBeEmpty();
}
