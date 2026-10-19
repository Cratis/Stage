// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_seeded_query_validation_rejections : Specification
{
    [Theory]
    [InlineData("given InvoiceIssued\n          for \"invoice-one\"")]
    [InlineData("given readmodel InvoiceSummary")]
    public void should_refuse_the_seeded_fixture_without_partial_artifacts(string given)
    {
        var source = a_validated_query_key_model.Source + "\n" + $$"""
                  specification InvalidQueryWithState
                    {{given}}
                      invoiceId = "invoice-one"
                      description = "North"
                    when query InvoiceById
                      invoiceId = "x"
                    then error
            """;
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        var diagnostic = plan.Diagnostics.Where(diagnostic => diagnostic.Severity != global::Cratis.Stage.Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Information).Single();
        diagnostic.Code.ShouldEqual("STAGE-ESM-011");
        diagnostic.Message.ShouldContain("only empty fixtures are supported");
        plan.Artifacts.ShouldBeEmpty();
    }
}
