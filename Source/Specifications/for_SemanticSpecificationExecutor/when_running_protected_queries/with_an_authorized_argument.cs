// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_protected_queries;

public class with_an_authorized_argument : a_projected_specification
{
    void Establish() => _plan = Compile(
        """
        specification RegisteringAndLookingUpAnOwnedInvoice
          given caller
            authenticated
            claim "owner" = "first"
          when append InvoiceRegistered
            for "first"
            description = "North"
          then query InvoiceById
            arguments
              invoiceId = "first"
            result
              description = "North"
        """,
        "require claim \"owner\" matches invoiceId");

    Task Because() => Run();

    [Fact] void should_use_the_query_argument_as_the_policy_artifact() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_match_the_reference() => _reference.Passed.ShouldBeTrue();
}
