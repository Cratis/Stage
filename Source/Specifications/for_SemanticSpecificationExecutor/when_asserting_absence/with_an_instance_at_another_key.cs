// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_asserting_absence;

public class with_an_instance_at_another_key : a_projected_specification
{
    void Establish() => _plan = Compile("""
        specification RegisteringAnotherInvoiceLeavesThisOneAbsent
          when append InvoiceRegistered
            for "second"
            description = "North"
          then no readmodel InvoiceView for "first"
        """);

    Task Because() => Run();

    [Fact] void should_check_only_the_asserted_key() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_match_the_reference() => _reference.Passed.ShouldBeTrue();
    [Fact] void should_materialize_the_other_instance() => _result.Trace!.ReadModels.Count.ShouldEqual(1);
}
