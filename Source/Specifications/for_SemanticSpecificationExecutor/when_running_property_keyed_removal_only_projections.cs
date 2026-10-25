// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_property_keyed_removal_only_projections : Specification
{
    internal const string Source = """
        module Billing
          feature Invoices
            slice StateChange RemoveInvoice
              command RemoveInvoice
                sourceId String identifier
                invoiceId String
                produces InvoiceRemoved
                  for sourceId
                  invoiceId = invoiceId
            slice StateView InvoiceLookup
              event InvoiceRemoved
                invoiceId String
              readmodel InvoiceView
                invoiceId String key
              projection Invoices => InvoiceView
                remove with InvoiceRemoved key invoiceId
              specification RemovingOneOfTwoInvoices
                given readmodel InvoiceView
                  invoiceId = "first"
                given readmodel InvoiceView
                  invoiceId = "second"
                when append InvoiceRemoved
                  invoiceId = "first"
                then no readmodel InvoiceView for "first"
                then readmodel InvoiceView
                  invoiceId = "second"
        """;

    SemanticExecutionPlan _plan = null!;

    void Establish() => _plan = Compile(Source);

    [Fact] public async Task should_remove_only_the_instance_named_by_the_property() => await ShouldPass(_plan);

    [Fact] public async Task should_isolate_each_run()
    {
        await ShouldPass(_plan);
        await ShouldPass(_plan);
    }

    [Fact] public async Task should_replace_property_keyed_history_before_applying_the_action()
    {
        var source = Source.Replace("        given readmodel InvoiceView\n          invoiceId = \"first\"", "        given InvoiceRemoved\n          for \"unrelated\"\n          invoiceId = \"second\"\n        given readmodel InvoiceView\n          invoiceId = \"first\"", StringComparison.Ordinal);
        await ShouldPass(Compile(source));
    }

    internal static SemanticExecutionPlan Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        return SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    static async Task ShouldPass(SemanticExecutionPlan plan)
    {
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
    }
}
