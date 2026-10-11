// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_protected_queries;

public class with_a_denial_after_a_command : a_projected_specification
{
    void Establish() => _plan = Compile(
        """
        specification AQueryDenialRejectsTheScenario
          given caller
            authenticated
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

    [Fact] void should_fail_the_success_expectation() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_match_the_reference() => _reference.Passed.ShouldBeFalse();
    [Fact] void should_not_expose_tentative_facts() => _result.Trace!.Facts.ShouldBeEmpty();
    [Fact] void should_not_expose_tentative_read_models() => _result.Trace!.ReadModels.ShouldBeEmpty();
}
