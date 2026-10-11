// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_asserting_absence;

public class without_a_materialized_instance : a_projected_specification
{
    void Establish() => _plan = Compile("""
        specification AnUnregisteredInvoiceIsAbsent
          then no readmodel InvoiceView for "first"
        """);

    Task Because() => Run();

    [Fact] void should_pass_the_absence_assertion() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_match_the_reference() => _reference.Passed.ShouldBeTrue();
    [Fact] void should_expose_no_materialized_instances() => _result.Trace!.ReadModels.ShouldBeEmpty();
}
