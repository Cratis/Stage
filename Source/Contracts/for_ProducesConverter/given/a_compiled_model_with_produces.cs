// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Contracts.for_ProducesConverter.given;

public class a_compiled_model_with_produces : Specification
{
    protected const string Source =
        """
        concept InvoiceId : Uuid
        concept CurrencyCode : String
        concept InvoiceStatus : Enum
          draft
          sent

        module Invoicing

          feature InvoiceManagement

            slice StateChange RegisterInvoice

              command RegisterInvoice
                invoiceId  InvoiceId
                currency   CurrencyCode
                isProForma Bool

                produces InvoiceRegistered
                  tag audit
                  invoiceId    = invoiceId
                  status       = "draft"
                  registeredAt = $context.occurred
                  source       = $env.SERVICE_NAME

                produces when isProForma == true
                  ProFormaInvoiceIssued
                    invoiceId = invoiceId

                produces when currency == "USD" or currency == "EUR"
                  ForeignCurrencyInvoiceRegistered
                    invoiceId = invoiceId
                    currency  = currency

              event InvoiceRegistered
                invoiceId    InvoiceId
                status       InvoiceStatus
                registeredAt DateTime
                source       String

              event ProFormaInvoiceIssued
                invoiceId InvoiceId

              event ForeignCurrencyInvoiceRegistered
                invoiceId InvoiceId
                currency  CurrencyCode
        """;

    protected EventModel _model = null!;
    protected IReadOnlyList<ProducedEvent> _produces = [];

    void Establish()
    {
        _model = EventModelLoader.LoadFromSource(Source);
        _produces = _model.Collections[0].Modules[0].Features[0].Slices
            .Single(slice => slice.Name == "RegisterInvoice").Command!.Produces;
    }
}
