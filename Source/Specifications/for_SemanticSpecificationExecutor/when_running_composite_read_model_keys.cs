// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_composite_read_model_keys : Specification
{
    const string Source = """
        concept InvoiceId : String
        concept PaymentMethod : Enum
          card
          cash
        module Billing
          feature Payments
            slice StateView Totals
              event PaymentReceived
                invoice InvoiceId
                method PaymentMethod
                amount Int
              readmodel PaymentTotals
                invoiceId InvoiceId key
                method PaymentMethod key
                paid Int
              query PaymentTotal => PaymentTotals optional
                by
                  invoiceId InvoiceId
                  method PaymentMethod
              projection PaymentTotalsProjection => PaymentTotals
                no automap
                from PaymentReceived
                  key
                    invoiceId = invoice
                    method = method
                  paid = amount
              specification ProjectsByParts
                when append PaymentReceived
                  invoice = "INV-1"
                  method = "card"
                  amount = 20
                then readmodel PaymentTotals
                  invoiceId = "INV-1"
                  method = "card"
                  paid = 20
        """;

    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunRecord _result = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("billing"), "billing", "Billing.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    async Task Because() => _result = (await new SemanticSpecificationExecutor().Run(_plan, new([]), new())).Results.Single();

    [Fact] void should_compile_at_v27() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V27);
    [Fact] void should_not_pass_the_specification() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_refuse_the_projection() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_name_composite_read_model_keys() => _result.Unsupported!.Details.ShouldContain("ESM v27 composite read-model keys");
}
