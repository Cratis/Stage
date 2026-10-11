// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_protected_queries;

public class with_an_admitted_command : a_projected_specification
{
    void Establish() => _plan = Compile(
        """
        specification RegisteringAndReadingAnInvoice
          given caller
            authenticated
            role "Staff"
          when RegisterInvoice
            invoiceId = "first"
            description = "North"
          then InvoiceRegistered
            for "first"
            description = "North"
          then query InvoiceById
            arguments
              invoiceId = "first"
            result
              description = "North"
        """,
        commandScenario: true);

    Task Because() => Run();

    [Fact] void should_compare_the_query_after_the_command() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_match_the_reference() => _reference.Passed.ShouldBeTrue();
    [Fact] void should_expose_the_appended_fact() => _result.Trace!.Facts.Count.ShouldEqual(1);
}
