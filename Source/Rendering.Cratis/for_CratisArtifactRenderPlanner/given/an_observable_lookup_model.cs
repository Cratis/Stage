// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class an_observable_lookup_model
{
    public static string Source => a_validated_query_key_model.Source
        .Replace("query InvoiceById => InvoiceSummary?", "query InvoiceById => observable InvoiceSummary?", StringComparison.Ordinal) + "\n" + """
              specification LookingUpIssuedInvoice
                given InvoiceIssued
                  for "invoice-one"
                  invoiceId = "invoice-one"
                  description = "North"
                given InvoiceIssued
                  for "invoice-two"
                  invoiceId = "invoice-two"
                  description = "South"
                then query InvoiceById
                  arguments
                    invoiceId = "invoice-one"
                  result
                    invoiceId = "invoice-one"
                    description = "North"
        """;
}
