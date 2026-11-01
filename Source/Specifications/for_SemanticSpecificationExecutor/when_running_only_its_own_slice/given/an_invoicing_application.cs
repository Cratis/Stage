// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_only_its_own_slice.given;

// Screenplay decision 0115: each slice's specification runs that slice only. Registering is reacted to and published by other
// slices; Collecting's reaction invokes CloseInvoice, which another slice owns and guards with a constraint.
public class an_invoicing_application : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecificationRunRecord _result = null!;
    protected SemanticSpecificationRun _reference = null!;
    protected int _worldFacts;

    protected void Compile(string collecting)
    {
        var source = $$"""
            module Billing
              feature Invoices
                slice StateChange RegisterInvoice
                  command RegisterInvoice
                    id String identifier
                    produces InvoiceRegistered
                      for id
                  event InvoiceRegistered
                  specification Registering
                    when RegisterInvoice
                      id = "one"
                    then InvoiceRegistered
                      for "one"
                slice Automation Welcome
                  event InvoiceWelcomed
                  reaction Welcomer
                    when InvoiceRegistered
                      produces InvoiceWelcomed
                        for "one"
                slice Translate PublishInvoice
                  direction outbound
                  public event InvoicePublished
                    status String
                  projection Publish => InvoicePublished
                    from InvoiceRegistered
                      status = "registered"
                slice StateChange CloseInvoice
                  command CloseInvoice
                    id String identifier
                    reason String
                    produces InvoiceClosed
                      for id
                      reason = reason
                  event InvoiceClosed
                    reason String
                  constraint ClosedOnce
                    unique event InvoiceClosed
                    message "Already closed"
                slice Automation Collecting
                  command PayInvoice
                    id String identifier
                    produces InvoicePaid
                      for id
                      id = id
                  event InvoicePaid
                    id String
                  event ClosingRefused
                    message String
                  reaction Closer
                    when InvoicePaid
                      id
                      invokes CloseInvoice
                        id = id
                        reason = "paid"
                        on refused by constraint ClosedOnce
                          produces ClosingRefused
                            for id
                            message = $refusal.message
                  specification Collecting
            {{string.Join('\n', collecting.Split('\n').Select(line => new string(' ', 8) + line))}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Invoicing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("invoicing"), "invoicing", "Invoicing.play", source);
        var compilation = new SemanticModelCompiler().Compile("Invoicing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    protected async Task Run(string name)
    {
        var specification = _plan.Specifications.Values.Single(candidate => candidate.Name == name);
        _reference = new SemanticSpecificationRunner().Run(_plan, specification.Id);
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (_, count) => _worldFacts = count };
        _result = (await executor.Run(_plan, new([specification.Id]), new())).Results.Single();
    }
}
