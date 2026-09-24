// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

// The rendered query allows anonymous callers, so a policy on it cannot be dropped.
public class with_an_authorized_query : given.an_invoice_model
{
    const string Lookup = """

          feature Lookups
            authorize Clerks
            slice StateView InvoiceLookup
              readmodel InvoiceSummary
                description String
              query InvoiceByDescription => InvoiceSummary?
                by description String
              projection InvoiceSummaryProjection => InvoiceSummary
                from InvoiceIssued key description
        """;

    void Because() => Plan(Policy + Invoices + Lookup);

    [Fact] void should_not_plan_the_application() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_the_unrendered_authorization() => ErrorCodes.ShouldContain("STAGE-ESM-015");
}
