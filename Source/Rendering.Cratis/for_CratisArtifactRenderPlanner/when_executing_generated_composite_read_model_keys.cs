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
        concept Reference : String
        concept PaymentMethod : Enum
          card
          cash
        module Billing
          feature Payments
            slice StateChange Record
              command RecordPayment
                invoice InvoiceId identifier
                method PaymentMethod
                reference Reference
                amount Int
                produces PaymentReceived
                  for invoice
                  invoice = invoice
                  method = method
                  reference = reference
                  amount = amount
              command RemovePayment
                invoice InvoiceId identifier
                method PaymentMethod
                reference Reference
                produces PaymentRemoved
                  for invoice
                  invoice = invoice
                  method = method
                  reference = reference
              event PaymentReceived
                invoice InvoiceId
                method PaymentMethod
                reference Reference
                amount Int
              event PaymentRemoved
                invoice InvoiceId
                method PaymentMethod
                reference Reference
              specification RecordsAPayment
                when RecordPayment
                  invoice = "INV-1"
                  method = "card"
                  reference = "R1"
                  amount = 12
                then PaymentReceived
                  for "INV-1"
                  invoice = "INV-1"
                  method = "card"
                  reference = "R1"
                  amount = 12
            slice StateView Totals
              readmodel PaymentTotals
                invoiceId InvoiceId key
                method PaymentMethod key
                reference Reference key
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
                    reference = reference
                  total = amount
                remove with PaymentRemoved
                  key
                    reference = reference
                    method = method
                    invoiceId = invoice
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_render_the_key_and_run_the_generated_specifications()
    {
        try
        {
            var projection = string.Concat(GeneratedFiles("*.cs").Where(path => !path.Contains("Specs", StringComparison.Ordinal) && !path.Contains("obj", StringComparison.Ordinal)).Select(ReadGeneratedFile));
            Assert.Contains("UsingCompositeKey<PaymentTotalsKey>", projection, StringComparison.Ordinal);
            Assert.Contains("public record PaymentTotalsKey(", projection, StringComparison.Ordinal);
            Assert.Contains("[global::Cratis.Chronicle.Keys.KeyAttribute] PaymentTotalsKey Id", projection, StringComparison.Ordinal);
            AddGeneratedSpecification("CompositeKeys.cs", CompositeKeySpecification);
            var build = await Run("composite-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var tests = await Run("composite-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", tests, StringComparison.Ordinal);
            Assert.DoesNotContain("Failed!", tests, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }

    // Runs the rendered projection under Chronicle's ReadModelScenario. The three parts are chosen so that any composition of the key
    // that joins or drops a part collapses distinct instances: ("A_x", card, "y") and ("A", card, "x_y") share an underscore-joined text.
    const string CompositeKeySpecification = """
        using Cratis.Specifications;
        using Projects.Billing.Payments.Record;
        using Projects.Billing.Payments.Totals;
        using Projects.Common;
        using Xunit;

        namespace Projects.CompositeKeys;

        public class when_projecting_composite_keyed_payments : Specification, IDisposable
        {
            readonly Cratis.Chronicle.Testing.ReadModels.ReadModelScenario<PaymentTotals> _scenario = new();

            async Task Establish()
            {
                await _scenario.Given.ForEventSource("INV-1").Events(
                    new PaymentReceived(new InvoiceId("A_x"), PaymentMethod.Card, new Reference("y"), 1),
                    new PaymentReceived(new InvoiceId("A"), PaymentMethod.Card, new Reference("x_y"), 2),
                    new PaymentReceived(new InvoiceId("A"), PaymentMethod.Cash, new Reference("x_y"), 4),
                    new PaymentReceived(new InvoiceId("B"), PaymentMethod.Card, new Reference("y"), 8),
                    new PaymentReceived(new InvoiceId("B"), PaymentMethod.Card, new Reference("gone"), 16),
                    new PaymentRemoved(new InvoiceId("B"), PaymentMethod.Card, new Reference("gone")));
            }

            PaymentTotals[] Instances => [.. _scenario.Instances.Values];

            PaymentTotals Find(string invoice, PaymentMethod method, string reference) =>
                Instances.Single(_ => _.InvoiceId.Value == invoice && _.Method == method && _.Reference.Value == reference);

            [Fact] void should_keep_one_instance_per_distinct_key() => Instances.Length.ShouldEqual(4);
            [Fact] void should_keep_the_underscore_boundary_apart_first() => Find("A_x", PaymentMethod.Card, "y").Total.ShouldEqual(1);
            [Fact] void should_keep_the_underscore_boundary_apart_second() => Find("A", PaymentMethod.Card, "x_y").Total.ShouldEqual(2);
            [Fact] void should_keep_instances_apart_by_the_enum_part() => Find("A", PaymentMethod.Cash, "x_y").Total.ShouldEqual(4);
            [Fact] void should_keep_an_instance_apart_by_the_concept_part() => Find("B", PaymentMethod.Card, "y").Total.ShouldEqual(8);
            [Fact] void should_remove_only_the_instance_whose_parts_all_match() => Instances.Any(_ => _.Reference.Value == "gone").ShouldBeFalse();
            [Fact] void should_carry_the_key_record_with_the_parts()
            {
                var key = Find("A", PaymentMethod.Cash, "x_y").Id;
                (key.InvoiceId.Value == "A" && key.Method == PaymentMethod.Cash && key.Reference.Value == "x_y").ShouldBeTrue();
            }

            public void Dispose() => _scenario.Dispose();
        }
        """;
}
#endif
