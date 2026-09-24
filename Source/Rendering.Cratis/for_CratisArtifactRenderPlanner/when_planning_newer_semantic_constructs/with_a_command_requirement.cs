// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

// A requirement rejects the command, so an application without it would append what the reference rejects.
public class with_a_command_requirement : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "        produces InvoiceIssued\n",
        "        validate\n          require description != \"rejected\"\n        produces InvoiceIssued\n",
        StringComparison.Ordinal));

    [Fact] void should_not_plan_the_application() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_the_unrendered_semantic() => ErrorCodes.ShouldContain("STAGE-ESM-005");
    [Fact] void should_not_plan_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
