// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_protected_queries;

public class with_an_authorized_subject : a_projected_specification
{
    void Establish() => _plan = Compile(
        """
        specification LookingUpAnOwnedInvoice
          given caller
            authenticated
            role "Staff"
            claim "owner" = "first"
          given InvoiceRegistered
            for "first"
            description = "North"
          when query InvoiceById
            invoiceId = "first"
          then result
            description = "North"
        """,
        "require authenticated and role \"Staff\" and claim \"owner\" matches subject");

    Task Because() => Run();

    [Fact] void should_compare_the_authorized_result() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_match_the_reference() => _reference.Passed.ShouldBeTrue();
    [Fact] void should_expose_the_query_result() => _result.Trace!.Queries.Count.ShouldEqual(1);
}
