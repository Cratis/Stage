// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_admitting_a_read_model_a_performer_answers : Specification
{
    SemanticRuntimeAdmission _admission = null!;
    SemanticExecutionPlan _plan = null!;

    void Establish()
    {
        const string source = """
            module Billing
              feature Invoices
                slice StateView InvoiceFigures
                  readmodel InvoiceFigures
                    invoiceId String
                    status String
                  query CurrentFigures => InvoiceFigures
                    by invoiceId String
                    performer
                      file Queries/CurrentFigures.cs
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("billing"), "billing", "Billing.play", source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    void Because() => _admission = new(_plan);

    [Fact] void should_not_refuse_the_application() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_list_the_read_model() => _admission.Entries.Count(entry => entry.Kind == "readModel").ShouldEqual(1);
    [Fact] void should_not_run_a_query_it_does_not_model() => _admission.Entries.Count(entry => entry.Kind == "query").ShouldEqual(0);
}
