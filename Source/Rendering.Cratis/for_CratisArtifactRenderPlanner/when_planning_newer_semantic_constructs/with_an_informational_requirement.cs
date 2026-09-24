// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_an_informational_requirement : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "        produces InvoiceIssued\n",
        "        validate\n          require description != \"draft\"\n            severity information\n        produces InvoiceIssued\n",
        StringComparison.Ordinal));

    [Fact] void should_not_plan_any_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_reject_the_severity_arc_does_not_block() => ErrorCodes.ShouldContainOnly(["STAGE-ESM-005"]);
}
