// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_nullable_query_validation_gaps : Specification
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_report_the_null_argument_validation_gap(bool key)
    {
        var source = a_validated_query_key_model.Source.Replace("min 2", "not empty", StringComparison.Ordinal);
        if (!key) source = source.Replace("query InvoicesById => observable InvoiceSummary[]\n        by invoiceId InvoiceId", "query InvoicesById => observable InvoiceSummary[]\n        filter invoiceId InvoiceId optional", StringComparison.Ordinal);
        var model = invoice_model.Compile(source);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var located = context.SelectedSlices().Single(value => value.Slice.Queries.Length > 0);
        var slice = key ? located.Slice with
        {
            Queries = [.. located.Slice.Queries.Select(query => query with { Argument = query.Argument! with { Type = query.Argument!.Type with { IsOptional = true } } })]
        } : located.Slice;
        var diagnostics = SemanticCratisAdmission.Evaluate(context, [located with { Slice = slice }]);
        diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-010" && diagnostic.Message.Contains("Arc skips validation for null arguments", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
