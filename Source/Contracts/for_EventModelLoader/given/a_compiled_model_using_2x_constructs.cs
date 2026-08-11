// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Contracts.for_EventModelLoader.given;

/// <summary>
/// A document exercising the constructs the contract gained for Screenplay 2.x — concept compliance attributes,
/// command authorization, <c>require</c>, <c>reads</c>, <c>produces … for</c>, event tags and specification read
/// model steps — compiled the same way the engine compiles one at startup.
/// </summary>
public class a_compiled_model_using_2x_constructs : Specification
{
    protected const string Source =
        """
        concept InvoiceId    : Uuid
        concept ContractId   : Uuid
        concept EmailAddress : String @pii
          pii reason "Billing contact - lawful basis: contract performance"

        concept BankAccount : String @pii @sensitive
          sensitive reason "A leaked account number enables direct financial harm"

        policy IsAccountant
          require role "Accountant"

        policy IsFinance
          require role "Finance"

        policy OwnsInvoice
          require authenticated

        module Invoicing

          feature Billing

            slice StateChange ActivateInvoice

              readmodel InvoiceScope
                isStarted Bool
                phase     String

              command ActivateInvoice
                invoiceId  InvoiceId identifier
                contractId ContractId
                email      EmailAddress
                account    BankAccount

                reads InvoiceScope by invoiceId

                authorize IsAccountant or IsFinance and OwnsInvoice

                validate
                  require InvoiceScope.isStarted == false
                    message "Already started"
                  require InvoiceScope.phase == "Contract"

                produces InvoiceActivated
                  invoiceId = invoiceId
                  email     = email

                produces ContractPolicyActivated
                  for contractId
                  contractId = contractId

              event InvoiceActivated
                invoiceId InvoiceId
                email     EmailAddress
                tag invoicing
                tag "billing"

              event ContractPolicyActivated
                contractId ContractId

              specification ActivatesAnInvoice
                given readmodel InvoiceScope
                  isStarted = false
                  phase     = "Contract"
                when ActivateInvoice
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then InvoiceActivated
                  invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then readmodel InvoiceScope
                  isStarted = true
        """;

    protected EventModel _model = null!;

    protected Slice _slice = null!;

    void Establish()
    {
        _model = EventModelLoader.LoadFromSource(Source);
        _slice = _model.Collections[0].Modules[0].Features[0].Slices.Single(slice => slice.Name == "ActivateInvoice");
    }
}
