// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class an_enumeration_model
{
    internal const string Source = """
        concept InvoiceId : String
        concept Status : Enum
          draft
          issued
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                id InvoiceId identifier
                status Status
                produces InvoiceIssued
                  for id
                  status = status
              event InvoiceIssued
                status Status
              specification IssuingInvoice
                when IssueInvoice
                  id = "invoice-one"
                  status = "issued"
                then InvoiceIssued
                  status = "issued"
        """;

    internal static ExecutableSemanticModel Compile(string? source = null) => invoice_model.Compile(source ?? Source);
}
