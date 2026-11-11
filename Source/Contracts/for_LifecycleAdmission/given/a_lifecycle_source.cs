// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;

/// <summary>
/// An invoice that is sent, goes overdue and is collected: a state machine and a workflow with an action, a decision, a timer,
/// a fan-out with its join, a wait for an event and an approval. Everything in it is something the runtime and the renderer run.
/// </summary>
public static class a_lifecycle_source
{
    /// <summary>The invoice that every specification of the source is about.</summary>
    public const string InvoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9";

    /// <summary>The Screenplay source.</summary>
    public const string Source =
        """
        concept InvoiceId : Uuid
        type InvoiceLine
          sku String
        policy IsController
          require role "Controller"
        persona Controller
          policy IsController
        module Billing
          feature Invoices
            slice StateChange RegisterInvoiceSlice
              command RegisterInvoice
                invoiceId InvoiceId identifier
                lines InvoiceLine[]
                produces InvoiceRegistered
                  for invoiceId
                  lines = lines
              event InvoiceRegistered
                lines InvoiceLine[]
            slice StateChange SendInvoiceSlice
              command SendInvoice
                invoiceId InvoiceId identifier
                produces InvoiceSent
                  for invoiceId
              event InvoiceSent
            slice StateChange MarkInvoiceOverdueSlice
              command MarkInvoiceOverdue
                invoiceId InvoiceId identifier
                produces InvoiceMarkedOverdue
                  for invoiceId
              event InvoiceMarkedOverdue
            slice StateChange PayInvoiceSlice
              command PayInvoice
                invoiceId InvoiceId identifier
                produces InvoicePaid
                  for invoiceId
              event InvoicePaid
            slice StateChange ChargeFeeSlice
              command ChargeFee
                invoiceId InvoiceId identifier
                produces LateFeeCharged
                  for invoiceId
              event LateFeeCharged
            slice StateChange TagInvoiceSlice
              command TagInvoice
                invoiceId InvoiceId identifier
                produces InvoiceTagged
                  for invoiceId
              event InvoiceTagged
            slice StateChange WriteOffSlice
              command WriteOff
                invoiceId InvoiceId identifier
                produces InvoiceWrittenOff
                  for invoiceId
              event InvoiceWrittenOff
            slice StateChange OfferPlanSlice
              command OfferPlan
                invoiceId InvoiceId identifier
                produces PaymentPlanOffered
                  for invoiceId
              event PaymentPlanOffered
            slice StateView InvoiceView
              readmodel Invoice
                invoiceId InvoiceId key
                status    String
                overdue   Bool
                lines     InvoiceLine[]
              projection InvoiceProjection => Invoice
                no automap
                from InvoiceRegistered
                  status  = "draft"
                  overdue = false
                  lines   = lines
                from InvoiceSent
                  status = "sent"
                from InvoiceMarkedOverdue
                  overdue = true
                from InvoicePaid
                  status = "paid"

        statemachine InvoiceLifecycle
          description "Where an invoice is"
          follows Invoice
          state Draft initial
            on InvoiceSent -> Sent
          state Sent
            entering invokes ChargeFee
            entering start Collect
            on InvoiceMarkedOverdue when overdue == true -> Overdue
            on InvoicePaid -> Paid
          state Overdue
            on InvoicePaid -> Paid
          state Paid final

          specification SendingStartsTheCollection
            description "Sending an invoice charges the fee and starts the collection"
            given InvoiceRegistered
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
              lines
                item
                  sku = "A"
                item
                  sku = "B"
            when append InvoiceSent
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            then is Sent
            then invokes ChargeFee
            then starts Collect

          specification AnOverdueInvoiceIsOverdue
            given InvoiceRegistered
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
              lines
                item
                  sku = "A"
                item
                  sku = "B"
            given InvoiceSent
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            when append InvoiceMarkedOverdue
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            then is Overdue

        workflow Collect
          description "Chases a sent invoice"
          for Invoice
          trigger when InvoiceLifecycle enters Sent
            -> ChargeLateFee
          action ChargeLateFee invokes ChargeFee attempts 3 timeout 30 seconds retry after 5 seconds
            next -> Pause
            failed -> Approve
          wait Pause for 2 days
            resumed -> Check
          decide Check when overdue == true
            "chase" -> EachLine
            "settled" -> Done
          foreach EachLine over lines parallel 2
            each -> TagLine
            completed -> Await
          action TagLine invokes TagInvoice
            next -> AllTagged
          join AllTagged all
          wait Await for InvoicePaid expires after 7 days
            resumed -> Done
            expired -> Approve
          approval Approve by Controller expires after 1 day
            approved -> Write
            rejected -> Plan
            expired -> Write
          action Write invokes WriteOff
          action Plan invokes OfferPlan
          action Done invokes TagInvoice

          specification APaidInvoiceIsTagged
            given InvoiceRegistered
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
              lines
                item
                  sku = "A"
                item
                  sku = "B"
            given InvoiceMarkedOverdue
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            when append InvoiceSent
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            outcome Await resumed
            then invokes ChargeFee
            then invokes TagInvoice
            then invokes TagInvoice
            then invokes TagInvoice
            then completes

          specification NoPaymentIsWrittenOff
            given InvoiceRegistered
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
              lines
                item
                  sku = "A"
                item
                  sku = "B"
            given InvoiceMarkedOverdue
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            when append InvoiceSent
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            outcome Await expired
            outcome Approve approved
            then invokes ChargeFee
            then invokes TagInvoice
            then invokes TagInvoice
            then invokes WriteOff
            then completes

          specification TheRunWaits
            given InvoiceRegistered
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
              lines
                item
                  sku = "A"
                item
                  sku = "B"
            given InvoiceMarkedOverdue
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            when append InvoiceSent
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            then invokes ChargeFee
            then invokes TagInvoice
            then invokes TagInvoice
            then waits at Await
        """;

    /// <summary>The Screenplay source of the same lifecycle limited to what the renderer emits: no decision, approval, fan-out or schedule.</summary>
    public const string RenderableSource =
        """
        concept InvoiceId : Uuid
        type InvoiceLine
          sku String
        policy IsController
          require role "Controller"
        persona Controller
          policy IsController
        module Billing
          feature Invoices
            slice StateChange RegisterInvoiceSlice
              command RegisterInvoice
                invoiceId InvoiceId identifier
                lines InvoiceLine[]
                produces InvoiceRegistered
                  for invoiceId
                  lines = lines
              event InvoiceRegistered
                lines InvoiceLine[]
            slice StateChange SendInvoiceSlice
              command SendInvoice
                invoiceId InvoiceId identifier
                produces InvoiceSent
                  for invoiceId
              event InvoiceSent
            slice StateChange MarkInvoiceOverdueSlice
              command MarkInvoiceOverdue
                invoiceId InvoiceId identifier
                produces InvoiceMarkedOverdue
                  for invoiceId
              event InvoiceMarkedOverdue
            slice StateChange PayInvoiceSlice
              command PayInvoice
                invoiceId InvoiceId identifier
                produces InvoicePaid
                  for invoiceId
              event InvoicePaid
            slice StateChange ChargeFeeSlice
              command ChargeFee
                invoiceId InvoiceId identifier
                produces LateFeeCharged
                  for invoiceId
              event LateFeeCharged
            slice StateChange TagInvoiceSlice
              command TagInvoice
                invoiceId InvoiceId identifier
                produces InvoiceTagged
                  for invoiceId
              event InvoiceTagged
            slice StateChange WriteOffSlice
              command WriteOff
                invoiceId InvoiceId identifier
                produces InvoiceWrittenOff
                  for invoiceId
              event InvoiceWrittenOff
            slice StateChange OfferPlanSlice
              command OfferPlan
                invoiceId InvoiceId identifier
                produces PaymentPlanOffered
                  for invoiceId
              event PaymentPlanOffered
            slice StateView InvoiceView
              readmodel Invoice
                invoiceId InvoiceId key
                status    String
                overdue   Bool
                lines     InvoiceLine[]
              projection InvoiceProjection => Invoice
                no automap
                from InvoiceRegistered
                  status  = "draft"
                  overdue = false
                  lines   = lines
                from InvoiceSent
                  status = "sent"
                from InvoiceMarkedOverdue
                  overdue = true
                from InvoicePaid
                  status = "paid"

        statemachine InvoiceLifecycle
          description "Where an invoice is"
          follows Invoice
          state Draft initial
            on InvoiceSent -> Sent
          state Sent
            entering invokes ChargeFee
            entering start Collect
            on InvoiceMarkedOverdue when overdue == true -> Overdue
            on InvoicePaid -> Paid
          state Overdue
            on InvoicePaid -> Paid
          state Paid final

          specification SendingStartsTheCollection
            description "Sending an invoice charges the fee and starts the collection"
            given InvoiceRegistered
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
              lines
                item
                  sku = "A"
                item
                  sku = "B"
            when append InvoiceSent
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            then is Sent
            then invokes ChargeFee
            then starts Collect

          specification AnOverdueInvoiceIsOverdue
            given InvoiceRegistered
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
              lines
                item
                  sku = "A"
                item
                  sku = "B"
            given InvoiceSent
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            when append InvoiceMarkedOverdue
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            then is Overdue

        workflow Collect
          description "Chases a sent invoice"
          for Invoice
          trigger when InvoiceLifecycle enters Sent
            -> ChargeLateFee
          action ChargeLateFee invokes ChargeFee attempts 3 timeout 30 seconds retry after 5 seconds
            next -> Pause
            failed -> Done
          wait Pause for 2 days
            resumed -> Await
          wait Await for InvoicePaid expires after 7 days
            resumed -> Done
            expired -> Write
          action Write invokes WriteOff
          action Done invokes TagInvoice

          specification APaidInvoiceIsTagged
            given InvoiceRegistered
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
              lines
                item
                  sku = "A"
            when append InvoiceSent
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            outcome Await resumed
            then invokes ChargeFee
            then invokes TagInvoice
            then completes

          specification NoPaymentIsWrittenOff
            given InvoiceRegistered
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
              lines
                item
                  sku = "A"
            when append InvoiceSent
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            outcome Await expired
            then invokes ChargeFee
            then invokes WriteOff
            then completes

        workflow Nudge
          description "Reminds about an invoice that went overdue"
          for Invoice
          trigger Overdue on InvoiceMarkedOverdue
            -> Remind
          action Remind invokes TagInvoice

          specification AnOverdueInvoiceIsTagged
            given InvoiceRegistered
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
              lines
                item
                  sku = "A"
            when append InvoiceMarkedOverdue
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            then invokes TagInvoice
            then completes
        """;

    /// <summary>
    /// Compiles the source to an execution plan.
    /// </summary>
    /// <param name="source">The Screenplay source; the invoice lifecycle when omitted.</param>
    /// <returns>The plan.</returns>
    public static SemanticExecutionPlan Plan(string source = Source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("InvoiceModel"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("invoices"), "invoices", "Invoices.play", source);
        var compilation = new SemanticModelCompiler().Compile("InvoiceModel", SemanticDocumentSet.Create([document], catalog));
        if (!compilation.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, compilation.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Severity} {diagnostic.Location}: {diagnostic.Message}")));
        }

        return SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }
}
