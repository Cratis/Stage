// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class a_projected_query_validation_model
{
    public static ExecutableSemanticModel Compile()
    {
        var source = Encoding.UTF8.GetString(QueryValidationCorpus.SourceForms.Single(form => form.Name == "single").Documents.Single().Bytes.AsSpan());
        const string change = """
            slice StateChange Issue
              command IssueInvoice
                number InvoiceNumber identifier
                customer CustomerCode
                produces InvoiceIssued
                  for number
                  number = number
                  customer = customer
              event InvoiceIssued
                number InvoiceNumber
                customer CustomerCode
            slice StateView Lookup
        """;
        const string projection = """
                  projection InvoiceProjection => Invoice
                    from InvoiceIssued key number
                      number = number
                      customer = customer
            """;

        return invoice_model.Compile(source.Replace("    slice StateView Lookup", string.Join('\n', change.Split('\n').Select(line => "    " + line)), StringComparison.Ordinal) + "\n" + projection);
    }
}
