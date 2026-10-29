// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

public static class a_protected_invoice_model
{
    public const string Source =
        """
        command reads protected
        module Billing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId String identifier
                produces InvoiceRegistered
                  for invoiceId
              event InvoiceRegistered
            slice StateChange IssueInvoice
              command IssueInvoice
                invoiceId String identifier
                reads InvoiceState by invoiceId
                validate
                  require InvoiceState.status == "draft"
                    message "Only a draft invoice can be issued"
                produces InvoiceIssued
                  for invoiceId
              event InvoiceIssued
              event InvoiceCancelled
              readmodel InvoiceState
                id String key
                status String
              projection InvoiceStateProjection => InvoiceState
                from InvoiceRegistered
                  id = $eventSourceId
                  status = "draft"
                from InvoiceIssued
                  status = "issued"
                remove with InvoiceCancelled
        """;
}
