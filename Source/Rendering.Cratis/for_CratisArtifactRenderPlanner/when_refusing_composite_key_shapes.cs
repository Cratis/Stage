// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_composite_key_shapes
{
    internal const string Valid = Base;

    const string Base = """
        concept InvoiceId : String
        concept Reference : String
        module Billing
          feature Payments
            slice StateChange Record
              command RecordPayment
                invoice InvoiceId identifier
                reference Reference
                amount Int
                at DateTime
                produces PaymentReceived
                  for invoice
                  invoice = invoice
                  reference = reference
                  amount = amount
                  at = at
              event PaymentReceived
                invoice InvoiceId
                reference Reference
                amount Int
                at DateTime
              event PaymentRemoved
                invoice InvoiceId
                reference Reference
            slice StateView Totals
              readmodel PaymentTotals
                invoiceId InvoiceId key
                reference Reference key
                total Int
              query TotalsByInvoice => PaymentTotals[]
                by invoiceId InvoiceId
              projection PaymentTotals => PaymentTotals
                no automap
                from PaymentReceived
                  key
                    invoiceId = invoice
                    reference = reference
                  total = amount
                remove with PaymentRemoved
                  key
                    invoiceId = invoice
                    reference = reference
        """;

    public static TheoryData<string, string, string> Shapes => new()
    {
        { "a colliding key type", Replace("        module Billing\n", "        concept PaymentTotalsKey : String\n        module Billing\n"), "collides with another declaration" },
        { "a protected live query", Replace(Replace("        module Billing\n", "        policy Owns\n          require role \"Staff\"\n        module Billing\n"), "              projection PaymentTotals", "              query LiveTotals => observable PaymentTotals[]\n                by invoiceId InvoiceId\n                authorize Owns\n              projection PaymentTotals"), "protected live queries" },
        { "a mapping onto a key part", Replace("                  total = amount\n", "                  total = amount\n                  reference = reference\n"), "cannot also set a key part" }
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public void should_refuse_with_the_reason(string shape, string source, string reason)
    {
        var plan = invoice_model.Plan(invoice_model.Compile(source));

        rendering_refusals.Rejected(plan, "STAGE-ESM-048");
        Assert.True(
            plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-048" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning && diagnostic.Message.Contains(reason, StringComparison.Ordinal)),
            $"{shape}: artifacts={plan.Artifacts.Length} refusals={string.Join(',', plan.Refusals.SelectMany(r => r.Codes))} gaps={plan.ImplementationGaps.Length} {string.Join(" | ", plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}"))}");
    }

    static string Replace(string from, string to) => Replace(Base + "\n", from, to);

    static string Replace(string source, string from, string to)
    {
        var result = source.Replace(Dedent(from), Dedent(to), StringComparison.Ordinal);
        Assert.True(source != result, Dedent(from));
        return result;
    }

    static string Dedent(string text) => ("\n" + text).Replace("\n        ", "\n", StringComparison.Ordinal)[1..];
}
