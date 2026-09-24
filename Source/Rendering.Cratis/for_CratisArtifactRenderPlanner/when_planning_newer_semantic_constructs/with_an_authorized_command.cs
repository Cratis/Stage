// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

// Caller fixtures remain blocked until generated specifications can inject a principal.
public class with_an_authorized_command : given.an_invoice_model
{
    void Because() => Plan(Policy + Invoices
        .Replace("        produces InvoiceIssued\n", "        authorize Clerks\n        produces InvoiceIssued\n", StringComparison.Ordinal)
        .Replace("when IssueInvoice\n", Caller, StringComparison.Ordinal));

    [Fact] void should_not_plan_the_application() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_the_unsupported_caller_fixture() => ErrorCodes.ShouldContain("STAGE-ESM-011");
    [Fact] void should_not_plan_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
