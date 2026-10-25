// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

// Chronicle #4122 indexes null as an empty-string claim, so an optional unique property is not portable.
public class with_an_append_constraint : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace("description String", "description String?", StringComparison.Ordinal).Replace(
        "      event InvoiceIssued\n",
        "      constraint UniqueDescription\n        unique description on InvoiceIssued\n      event InvoiceIssued\n",
        StringComparison.Ordinal));

    [Fact] void should_record_partial_admission() => _plan.Outcome.ShouldEqual(global::Cratis.Stage.Contracts.Rendering.ArtifactRenderOutcome.Partial);
    [Fact] void should_report_the_unrendered_semantic() => RefusalCodes.ShouldContain("STAGE-ESM-014");
    [Fact] void should_omit_the_refused_parts_and_compile_the_rest() => AssertOmittedParts();
}
