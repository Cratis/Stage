// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Contracts.for_EventModelLoader.given;

public class a_compiled_invoicing_model : Specification
{
    protected const string Source =
        """
        concept InvoiceId : Uuid
        concept InvoiceNumber : String
        concept Money : Decimal
        concept Quantity : Int

        concept InvoiceStatus : Enum
          draft
          sent
          paid

        module Invoicing

          feature InvoiceManagement

            slice StateChange RegisterInvoice

              command RegisterInvoice
                invoiceId      InvoiceId
                invoiceNumber  InvoiceNumber
                amount         Money
                quantity       Quantity
                note           String?

                validate
                  invoiceNumber not empty                 message "Invoice number is required"
                  invoiceNumber matches "^INV-[0-9]{6}$"  message "Bad format"
                  quantity > 0                            message "Must be positive"

                produces InvoiceRegistered
                  invoiceId     = invoiceId
                  invoiceNumber = invoiceNumber
                  amount        = amount
                  status        = "draft"

              event InvoiceRegistered
                invoiceId     InvoiceId
                invoiceNumber InvoiceNumber
                amount        Money
                status        InvoiceStatus

              constraint UniqueInvoiceNumber
                unique invoiceNumber on InvoiceRegistered

              constraint OneRegistrationPerInvoice
                unique event InvoiceRegistered

              specification RegistersAnInvoice
                when RegisterInvoice
                  invoiceNumber = "INV-000001"
                then InvoiceRegistered
                  invoiceNumber = "INV-000001"

            slice StateView InvoiceList

              query ListInvoices => InvoiceListReadModel[]
                filter status InvoiceStatus?

              projection InvoiceList => InvoiceListReadModel
                from InvoiceRegistered key invoiceId
                  invoiceNumber = invoiceNumber
                  status        = "draft"
                  lineCount     = 0
                from InvoiceStatusChanged
                  status = status

            slice Automation NotifyOnRegistered

              reactor Notifier
                on InvoiceRegistered
                  file Reactors/Notifier.cs
        """;

    protected EventModel _model = null!;

    void Establish() => _model = EventModelLoader.LoadFromSource(Source);
}
