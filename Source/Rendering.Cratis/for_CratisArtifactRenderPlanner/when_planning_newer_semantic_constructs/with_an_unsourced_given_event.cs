// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_an_unsourced_given_event : an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "when IssueInvoice\n          description = \"First payload\"",
        "given InvoiceIssued\n          description = \"Prior payload\"\n        when IssueInvoice\n          description = \"First payload\"",
        StringComparison.Ordinal));

    [Fact] void should_reject_the_ambiguous_destination() => ErrorCodes.ShouldContain("STAGE-ESM-011");
    [Fact] void should_not_emit_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
