// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider.given;

public class a_composite_key_runtime : Specification
{
    protected SemanticRuntimeQueryPerformerProvider _provider = null!;
    protected IQueryPerformer _performer = null!;
    protected object? _result;
    ISemanticRuntime _runtime = null!;

    async Task Establish()
    {
        const string source = """
            concept InvoiceId : String
              validate
                min 3 message "Invoice id is too short"
            concept PaymentMethod : Enum
              card
              cash
            module Billing
              feature Payments
                slice StateChange Receive
                  command ReceivePayment
                    paymentId String identifier
                    invoice InvoiceId
                    method PaymentMethod
                    amount Int
                    produces PaymentReceived
                      for paymentId
                      invoice = invoice
                      method = method
                      amount = amount
                  event PaymentReceived
                    invoice InvoiceId
                    method PaymentMethod
                    amount Int
                slice StateChange Remove
                  command RemovePayment
                    paymentId String identifier
                    invoice InvoiceId
                    method PaymentMethod
                    produces PaymentRemoved
                      for paymentId
                      invoice = invoice
                      method = method
                  event PaymentRemoved
                    invoice InvoiceId
                    method PaymentMethod
                slice StateView Totals
                  readmodel PaymentTotals
                    invoiceId InvoiceId key
                    method PaymentMethod key
                    total Int
                  query Total => PaymentTotals optional
                    by
                      invoiceId InvoiceId
                      method PaymentMethod
                  query TotalsForInvoice => PaymentTotals[]
                    filter invoiceId InvoiceId
                    filter method PaymentMethod optional
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
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("billing"), "billing", "Billing.play", source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        var facts = new List<SemanticFact>();
        ((ISemanticFactTail)appender).Tail().Returns(_ => facts.Count == 0 ? ulong.MaxValue : (ulong)facts.Count - 1);
        appender.Append(Arg.Do<IReadOnlyList<SemanticFact>>(facts.AddRange), Arg.Any<SemanticCommandOccurrence>()).Returns(Task.CompletedTask);
        _runtime = SemanticRuntimeHosting.Create(plan, appender);
        var command = plan.Commands.Values.Single(candidate => candidate.Name == "ReceivePayment");
        foreach (var (paymentId, invoice, method, amount) in new[] { ("payment-1", "INV-1", "card", 12), ("payment-2", "INV-1", "cash", 8) })
        {
            var outcome = await _runtime.Execute(
                command,
                new Dictionary<string, JsonElement>
                {
                    ["paymentId"] = JsonSerializer.SerializeToElement(paymentId),
                    ["invoice"] = JsonSerializer.SerializeToElement(invoice),
                    ["method"] = JsonSerializer.SerializeToElement(method),
                    ["amount"] = JsonSerializer.SerializeToElement(amount)
                },
                new ClaimsPrincipal(),
                new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"),
                false);
            outcome.ShouldBeOfExactType<SemanticAccepted>();
        }

        _provider = new([_runtime], [new DynamicTypeFactory()], [new HttpContextAccessor { HttpContext = new DefaultHttpContext() }]);
    }

    protected async Task Perform(string name, QueryArguments arguments)
    {
        _performer = _provider.Performers.Single(performer => performer.Name == name);
        _result = await _performer.Perform(new QueryContext(_performer.FullyQualifiedName, CorrelationId.NotSet, Paging.NotPaged, Sorting.None, arguments));
    }
}
