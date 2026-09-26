// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_command_occurrence_mapping : given.an_invoice_model
{
    void Because() => Plan(Invoices
        .Replace("          description = description\n", "          description = description\n          issuedAt = $context.occurred\n", StringComparison.Ordinal)
        .Replace("event InvoiceIssued\n        description String\n", "event InvoiceIssued\n        description String\n        issuedAt DateTime\n", StringComparison.Ordinal)
        .Replace("then InvoiceIssued\n          description = \"First payload\"", "then InvoiceIssued\n          description = \"First payload\"\n          issuedAt = \"2026-01-01T00:00:00.0000000+00:00\"", StringComparison.Ordinal)
        .Replace("then InvoiceIssued\n          description = \"Second payload\"", "then InvoiceIssued\n          description = \"Second payload\"\n          issuedAt = \"2026-01-01T00:00:00.0000000+00:00\"", StringComparison.Ordinal));

    [Fact] void should_reject_fixed_expectations_for_an_uncontrolled_occurrence() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_specification_admission() => ErrorCodes.ShouldContain("STAGE-ESM-011");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
