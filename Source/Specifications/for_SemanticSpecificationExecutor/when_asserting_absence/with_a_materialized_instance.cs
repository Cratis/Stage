// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_asserting_absence;

public class with_a_materialized_instance : a_projected_specification
{
    void Establish() => _plan = Compile("""
        specification ARegisteredInvoiceIsNotAbsent
          given InvoiceRegistered
            for "first"
            description = "North"
          then no readmodel InvoiceView for "first"
        """);

    Task Because() => Run();

    [Fact] void should_fail_the_absence_assertion() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_match_the_reference() => _reference.Passed.ShouldBeFalse();
    [Fact] void should_preserve_the_reference_failure() => _result.Failures.ShouldContainOnly(_reference.Failures);
}
