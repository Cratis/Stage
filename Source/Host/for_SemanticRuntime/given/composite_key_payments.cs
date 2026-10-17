// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

public static class composite_key_payments
{
    public const string Source = """
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
}
