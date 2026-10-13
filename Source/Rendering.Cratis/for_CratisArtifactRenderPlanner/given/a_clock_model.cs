// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class a_clock_model
{
    public const string Source = """
        concept InvoiceId : String
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                invoiceId InvoiceId identifier
                produces event InvoiceIssued
                  issuedAt DateTime = $context.occurred
              specification AtTheAuthoredInstant
                given clock "2027-03-04T12:00:00.123+02:00"
                when IssueInvoice
                  invoiceId = "invoice-one"
                then InvoiceIssued
                  issuedAt = "2027-03-04T10:00:00.123Z"
        """;
}
