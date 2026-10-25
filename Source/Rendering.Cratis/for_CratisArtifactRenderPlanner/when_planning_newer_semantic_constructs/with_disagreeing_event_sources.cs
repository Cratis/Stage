// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_disagreeing_event_sources : given.an_invoice_model
{
    void Because() => Plan(Invoices
        .Replace("when IssueInvoice\n          description = \"First payload\"", "when IssueInvoice\n          for \"invoice-one\"\n          description = \"First payload\"", StringComparison.Ordinal)
        .Replace(FirstExpectation, "then InvoiceIssued\n          for \"invoice-stated\"\n          description = \"First payload\"", StringComparison.Ordinal));

    [Fact] void should_record_partial_admission() => _plan.Outcome.ShouldEqual(global::Cratis.Stage.Contracts.Rendering.ArtifactRenderOutcome.Partial);
    [Fact] void should_reject_the_specification() => RefusalCodes.ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_omit_the_refused_parts_and_compile_the_rest() => AssertOmittedParts();
}
