// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands.for_SemanticQueryValidation.given;

public class a_multipart_query : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticKeyedQuery _query = null!;
    protected SemanticId _invoice;
    protected SemanticId _method;

    void Establish()
    {
        const string source = """
            concept InvoiceId : String
              validate
                min 3 message "Invoice id is too short"
            concept MethodCode : String
              validate
                min 2 message "Method is too short"
            concept RegionCode : String
              validate
                min 2 message "Region is too short"
            module Billing
              feature Payments
                slice StateView Totals
                  readmodel PaymentTotals
                    invoiceId InvoiceId key
                    method MethodCode key
                    paid Int
                  query Total => PaymentTotals optional
                    by
                      invoiceId InvoiceId
                      method MethodCode
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("MultipartQueries"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("queries"), "queries", "Queries.play", source);
        var compiled = new SemanticModelCompiler().Compile("MultipartQueries", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(';', compiled.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
        var query = _plan.Queries.Values.Single();
        var invoice = query.KeyParts.Single(part => part.Name == "invoiceId");
        var method = query.KeyParts.Single(part => part.Name == "method");
        _query = query with { KeyParts = [invoice, method] };
        _invoice = invoice.KeyProperty;
        _method = method.KeyProperty;
    }
}
