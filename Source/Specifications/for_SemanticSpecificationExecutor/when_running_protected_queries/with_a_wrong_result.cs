// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_protected_queries;

public class with_a_wrong_result : a_projected_specification
{
    void Establish() => _plan = Compile("""
        specification AuthorizationDoesNotMakeAWrongResultPass
          given caller
            authenticated
            role "Staff"
          given InvoiceRegistered
            for "first"
            description = "North"
          when query InvoiceById
            invoiceId = "first"
          then result
            description = "South"
        """);

    Task Because() => Run();

    [Fact] void should_fail_the_wrong_result() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_match_the_reference() => _reference.Passed.ShouldBeFalse();
}
