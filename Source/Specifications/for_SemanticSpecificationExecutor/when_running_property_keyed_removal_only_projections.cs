// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_property_keyed_removal_only_projections : Specification
{
    const string Source = """
        module Billing
          feature Invoices
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

    SemanticSpecificationOutcome _outcome;
    StageExecutionCapability? _capability;

    async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        _outcome = result.Outcome;
        _capability = result.Unsupported?.Capability;
    }

    [Fact] void should_refuse_the_specification() => _outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_refuse_it_as_a_projection_limit() => _capability.ShouldEqual(StageExecutionCapability.Projection);
}
