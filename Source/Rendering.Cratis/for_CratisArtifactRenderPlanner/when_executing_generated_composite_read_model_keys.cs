// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_composite_read_model_keys : a_generated_application
{
    internal const string Source = """
        concept InvoiceId : String
        concept PaymentMethod : Enum
          card
          cash
        module Billing
          feature Payments
            slice StateChange Record
              command RecordPayment
                invoice InvoiceId identifier
                method PaymentMethod
                amount Int
                produces PaymentReceived
                  for invoice
                  invoice = invoice
                  method = method
                  amount = amount
              command RemovePayment
                invoice InvoiceId identifier
                method PaymentMethod
                produces PaymentRemoved
                  for invoice
                  invoice = invoice
                  method = method
              event PaymentReceived
                invoice InvoiceId
                method PaymentMethod
                amount Int
              event PaymentRemoved
                invoice InvoiceId
                method PaymentMethod
              specification RecordsAPayment
                when RecordPayment
                  invoice = "INV-1"
                  method = "card"
                  amount = 12
                then PaymentReceived
                  for "INV-1"
                  invoice = "INV-1"
                  method = "card"
                  amount = 12
            slice StateView Totals
              readmodel PaymentTotals
                invoiceId InvoiceId key
                method PaymentMethod key
                total Int
              query TotalsForInvoice => PaymentTotals[]
                filter invoiceId InvoiceId
                filter method PaymentMethod optional
              query TotalsByInvoice => PaymentTotals[]
                by invoiceId InvoiceId
              projection PaymentTotals => PaymentTotals
                no automap
                from PaymentReceived
                  key
                    invoiceId = invoice
                    method = method
                  total = amount
                remove with PaymentRemoved
                  key
                    method = method
                    invoiceId = invoice
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_render_the_key_and_run_the_generated_specifications()
    {
        try
        {
            var build = await Run("composite-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var tests = await Run("composite-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", tests, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
