// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

// With a single expected event the order qualifier cannot change the outcome, so the specification renders.
public class with_order_independent_expectations : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(FirstExpectation, "then events in any order\n        " + FirstExpectation, StringComparison.Ordinal));

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_assert_the_expected_event() => Artifact("when_issuing_first_invoice.cs").ShouldContain("should_have_appended_invoice_issued");
}
