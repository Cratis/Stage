// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

public class a_universal_projection : a_projected_specification
{
    protected static SemanticExecutionPlan CreatePlan(bool all, string expected = "audit-2", string mapping = "$eventSourceId", bool expectRejection = false)
    {
        var unrelated = all ? $$"""
                    then readmodel InvoiceView
                      invoiceId = "audit-2"
                      lastSeen = "{{expected}}"
            """ : """
                    then no readmodel InvoiceView for "audit-2"
            """;
        var scenario = expectRejection ? """
                  specification ExpectingRejection
                    when RegisterInvoice
                      invoiceId = "invoice-1"
                      description = "North"
                    then error "A rejection was expected"
            """ : $$"""
                  specification RegisteringAnInvoice
                    given AuditRecorded
                      for "audit-2"
                      message = "unrelated"
                    when RegisterInvoice
                      invoiceId = "invoice-1"
                      description = "North"
                    then InvoiceRegistered
                      for "invoice-1"
                      description = "North"
                    then readmodel InvoiceView
                      invoiceId = "invoice-1"
                      lastSeen = "{{(mapping == "\"fixed\"" ? "fixed" : "invoice-1")}}"
            {{unrelated}}
            """;
        var source = $$"""
            module Billing
              feature Invoices
                slice StateChange Registration
                  command RegisterInvoice
                    invoiceId String identifier
                    description String
                    produces InvoiceRegistered
                      for invoiceId
                      description = description
                  event InvoiceRegistered
                    description String
                  command RecordAudit
                    auditId String identifier
                    message String
                    produces AuditRecorded
                      for auditId
                      message = message
                  event AuditRecorded
                    message String
            {{scenario}}
                slice StateView Lookup
                  readmodel InvoiceView
                    invoiceId String
                    lastSeen String
                  query InvoiceById => InvoiceView optional
                    by invoiceId String
                  projection InvoiceViews => InvoiceView
                    from InvoiceRegistered
                      invoiceId = $eventSourceId
                      lastSeen = description
                    {{(all ? "all" : "every")}}
            {{(all ? string.Empty : "          exclude children")}}
                      lastSeen = {{mapping}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        var admitted = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.NotNull(admitted.Plan);

        return admitted.Plan!;
    }
}
