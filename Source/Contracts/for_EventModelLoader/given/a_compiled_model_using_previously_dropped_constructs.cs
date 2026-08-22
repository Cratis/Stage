// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Contracts.for_EventModelLoader.given;

/// <summary>
/// A document exercising the constructs the contract had no member for — concepts, policies, reactions, screens
/// and captures — compiled the same way the engine compiles one at startup.
/// </summary>
/// <remarks>
/// Every one of these compiled cleanly before and arrived nowhere: on a captured 241-slice application the
/// import counted 160 concepts, 9 policies, 23 reactions and 205 screens on the way in, and none of them on the
/// way out. That is what makes a document that declares them and a model that holds nothing the interesting
/// case to pin down (Cratis/Stage#23).
/// </remarks>
public class a_compiled_model_using_previously_dropped_constructs : Specification
{
    protected const string Source =
        """
        concept InvoiceId  : Uuid
        concept CustomerId : Uuid

        concept EmailAddress : String @pii
          pii reason "Billing contact - lawful basis: contract performance"

        concept InvoiceStatus : Enum
          draft
          sent
          paid

        policy IsAuthenticated
          require authenticated

        policy IsAccountant
          require role "Accountant"

        policy IsFinanceDepartment
          require claim "department" matches "Finance"

        policy OwnsInvoice
          require claim "customerId" matches subject

        policy CanWriteOff
          require ( role "InvoiceManager" or role "Accountant" ) and authenticated

        policy IsAdultCustomer
          csharp
            ```
            return true;
            ```

        module Invoicing

          screen template MasterDetail
            fits slot content

            sidebar
            main

          feature Billing

            slice StateChange RegisterInvoice

              command RegisterInvoice
                invoiceId InvoiceId identifier

                produces InvoiceRegistered
                  invoiceId = invoiceId

              command CancelInvoice
                invoiceId InvoiceId identifier

              event InvoiceRegistered
                invoiceId InvoiceId

            slice Automation ChaseOverdueInvoices

              reaction OverdueChaser
                description "Reminds the billing contact while an invoice is late"
                at 08:00
                  produces InvoiceMarkedOverdue
                    invoiceId = invoiceId
                every 15 minutes
                when InvoiceRegistered
                  description "Re-checks whether a late payment has since arrived"
                  invoiceId
                  file Reactions/Chase.cs
                  invokes CancelInvoice
                    invoiceId = invoiceId
                where invoiceId != "none"

              event InvoiceMarkedOverdue
                invoiceId InvoiceId

            slice Translate LegacyInvoiceSync

              capture LegacyInvoiceCapture
                source api
                  route /invoices
                  poll 5m
                key id
                map
                  status = status translate
                    "utkast" => draft
                    "sendt"  => sent
                  split contactName by ","
                    firstName
                    lastName
                append InvoiceStatusChanged
                  tag legacy
                  when status
                    invoiceId = $.id
                append InvoicePaidFromSent
                  when status from "sent" to "paid"
                    invoiceId = $.id
                children lineItems identified by lineNumber
                  append InvoiceLineItemAdded
                    when added
                      invoiceId = $.id
                nested billingContact
                  append BillingContactUpdated
                    when email
                      invoiceId = $.id

              event InvoiceStatusChanged
                invoiceId InvoiceId

              event InvoicePaidFromSent
                invoiceId InvoiceId

              event InvoiceLineItemAdded
                invoiceId InvoiceId

              event BillingContactUpdated
                invoiceId InvoiceId

            slice StateView InvoiceList

              query ListInvoices => InvoiceListReadModel[]

              query GetInvoice => InvoiceListReadModel

              projection InvoiceList => InvoiceListReadModel
                from InvoiceRegistered
                  key invoiceId

              screen InvoiceListScreen
                title "Invoices"
                data InvoiceListReadModel[] via query ListInvoices
                action RegisterInvoice
                  navigate to InvoiceDetailScreen
                  label "Register"

              screen InvoiceDetailScreen
                template MasterDetail
                  sidebar
                    data InvoiceListReadModel via query GetInvoice by invoiceId
                  main
                    section actions
                      action CancelInvoice

              screen ExternalScreen
                file Screens/External.tsx
        """;

    protected EventModel _model = null!;

    protected Slice _registerInvoice = null!;

    protected Slice _chaseOverdueInvoices = null!;

    protected Slice _legacyInvoiceSync = null!;

    protected Slice _invoiceList = null!;

    void Establish()
    {
        _model = EventModelLoader.LoadFromSource(Source);

        _registerInvoice = Slice("RegisterInvoice");
        _chaseOverdueInvoices = Slice("ChaseOverdueInvoices");
        _legacyInvoiceSync = Slice("LegacyInvoiceSync");
        _invoiceList = Slice("InvoiceList");
    }

    Slice Slice(string name) => _model.Collections[0].Modules[0].Features[0].Slices.Single(slice => slice.Name == name);
}
