// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_concept_rules_and_a_requirement : given.an_invoice_model
{
    void Because() => Plan(("concept Display : String\n  validate\n    min 2\n" + Invoices)
        .Replace("        produces InvoiceIssued\n", "        display Display\n        validate\n          require description != \"draft\"\n        produces InvoiceIssued\n", StringComparison.Ordinal)
        .Replace("          streamReference = ", "          display = \"xy\"\n          streamReference = ", StringComparison.Ordinal));

    [Fact] void should_reject_the_unreproducible_failure_order() => ErrorCodes.ShouldContainOnly(["STAGE-ESM-005"]);
    [Fact] void should_not_plan_any_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
