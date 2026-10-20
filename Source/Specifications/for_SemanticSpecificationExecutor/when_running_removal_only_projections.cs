// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_removal_only_projections : Specification
{
    const string Source = """
        concept InvoiceId : String
        module Billing
          feature Invoices
            slice StateChange RemoveInvoice
              command RemoveInvoice
                invoiceId InvoiceId identifier
                produces InvoiceRemoved
                  for invoiceId
                  invoiceId = invoiceId
              event InvoiceRemoved
                invoiceId InvoiceId
            slice StateView InvoiceLookup
              readmodel InvoiceView
                invoiceId InvoiceId key
              projection Invoices => InvoiceView
                remove with InvoiceRemoved
              specification RemovingOneOfTwoInvoices
                given readmodel InvoiceView
                  invoiceId = "first"
                given readmodel InvoiceView
                  invoiceId = "second"
                when append InvoiceRemoved
                  for "first"
                  invoiceId = "first"
                then no readmodel InvoiceView for "first"
                then readmodel InvoiceView
                  invoiceId = "second"
        """;

    SemanticSpecificationOutcome _outcome;
    string _problem = string.Empty;

    async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        _outcome = result.Outcome;
        _problem = $"{result.Unsupported?.Details} {string.Join(';', result.Failures)}";
    }

    [Fact] void should_pass() => Assert.True(_outcome == SemanticSpecificationOutcome.Passed, $"{_outcome}: {_problem}");
}
