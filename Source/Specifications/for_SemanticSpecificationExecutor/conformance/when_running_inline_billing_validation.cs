// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.conformance;

// Screenplay v4.24.0's billing validation vector, compiled from source instead of hand-building ESM.
public class when_running_inline_billing_validation : Specification
{
    public const string Source =
        """
        concept InvoiceId : Uuid
        concept InvoiceNumber : String
          validate
            not empty message "An invoice needs a number"
            length == 10
        concept Money : Decimal
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId InvoiceId identifier
                invoiceNumber InvoiceNumber
                amount Money
                validate
                  amount > 0 message "An invoice for nothing is not an invoice"
                produces InvoiceRegistered
                  for invoiceId
                  invoiceId = invoiceId
                  invoiceNumber = invoiceNumber
                  amount = amount
              event InvoiceRegistered
                invoiceId InvoiceId
                invoiceNumber InvoiceNumber
                amount Money
              specification RegisteringAnInvoice
                when RegisterInvoice
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  invoiceNumber = "INV-000123"
                  amount = 1500
                then InvoiceRegistered
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  invoiceNumber = "INV-000123"
                  amount = 1500
              specification RejectingAnInvoiceForNothing
                when RegisterInvoice
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  invoiceNumber = "INV-000124"
                  amount = 0
                then error "An invoice for nothing is not an invoice"
              specification RejectingAShortInvoiceNumber
                when RegisterInvoice
                  invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                  invoiceNumber = "INV-1"
                  amount = 1500
                then error "A value must be exactly 10 characters long."
        """;

    readonly List<string> _differences = [];

    [Fact]
    public void should_match_all_reference_outcomes_and_messages() => Assert.True(_differences.Count == 0, string.Join(Environment.NewLine, _differences));

    protected async Task Because()
    {
        const string stableKey = "billing-validation";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(stableKey), stableKey, "RegisterInvoice.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        foreach (var specification in plan.Specifications.Values)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
            var record = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
            if (!reference.Passed || record.Outcome != SemanticSpecificationOutcome.Passed ||
                record.ExecutionKind != reference.Execution.Kind.ToString() ||
                (reference.Execution is SemanticRejected rejected && record.Trace?.Rejection != rejected.Details))
            {
                _differences.Add($"{specification.Name}: reference {reference.Execution.Kind}/{reference.Passed} {string.Join(';', reference.Failures)}; Stage {record.Outcome}/{record.ExecutionKind} {string.Join(';', record.Failures)} {record.Unsupported?.Details}");
            }
        }
    }
}
