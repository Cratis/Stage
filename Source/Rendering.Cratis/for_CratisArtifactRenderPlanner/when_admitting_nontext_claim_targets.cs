// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_admitting_nontext_claim_targets : Specification
{
    internal const string Source = """
        concept InvoiceId : Decimal
        policy Owns
          require claim "owner" matches subject or role "Staff"
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                authorize Owns
                invoiceId InvoiceId identifier
                description String
                produces InvoiceIssued
                  for invoiceId
                  invoiceId = invoiceId
                  description = description
              event InvoiceIssued
                invoiceId InvoiceId
                description String
        """;

    [Theory]
    [InlineData("Decimal")]
    [InlineData("Int")]
    [InlineData("Bool")]
    public void should_admit_a_claim_against_a_nontext_subject(string primitive) =>
        invoice_model.Plan(invoice_model.Compile(Source.Replace("concept InvoiceId : Decimal", $"concept InvoiceId : {primitive}", StringComparison.Ordinal))).Success.ShouldBeTrue();

    [Theory]
    [InlineData("Decimal")]
    [InlineData("Int")]
    [InlineData("Bool")]
    public void should_admit_a_claim_against_a_nontext_property(string primitive)
    {
        var source = when_rendering_portable_authorization.Source.Replace("description String", $"description {primitive}", StringComparison.Ordinal);
        invoice_model.Plan(invoice_model.Compile(source)).Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Date")]
    [InlineData("DateTime")]
    public void should_keep_refusing_text_backed_dates_without_a_faithful_text_renderer(string primitive)
    {
        var source = when_rendering_portable_authorization.Source.Replace("description String", $"description {primitive}", StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-015");
        plan.Artifacts.ShouldBeEmpty();
    }
}
