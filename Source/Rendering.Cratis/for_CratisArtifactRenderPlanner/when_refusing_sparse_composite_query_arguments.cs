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

public class when_refusing_sparse_composite_query_arguments : Specification
{
    ArtifactRenderDiagnostic[] _diagnostics = [];

    void Because()
    {
        var model = invoice_model.Compile("""
            concept InvoiceId : String
              validate
                min 2
            type InvoiceKey
              invoiceId InvoiceId
              note String optional
            module Billing
              feature Invoicing
                slice StateView Lookup
                  readmodel InvoiceSummary
                    invoiceId InvoiceId
                    invoiceKey InvoiceKey
                  query InvoicesByKey => InvoiceSummary[]
                    by invoiceKey InvoiceKey
            """);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var located = context.SelectedSlices().Single();
        var original = a_projected_query_validation_model.Compile().Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Specifications).Single(specification => specification.Name == "InvalidKey");
        var query = located.Slice.Queries.Single();
        var keyType = model.Application.Types.Single();
        var specification = original with
        {
            ThenQueries = [original.ThenQueries.Single() with
            {
                Query = query.Id,
                Key = new SemanticCompositeValue([new(keyType.Properties.Single(property => property.Name == "invoiceId").Id, SemanticValue.Text("A"))])
            }]
        };
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        SemanticSpecificationAdmission.Validate(context, located.Slice with { Specifications = [specification] }, diagnostics);
        _diagnostics = [.. diagnostics];
    }

    [Fact] void should_refuse_before_constructing_a_composite_literal() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-011");
    [Fact] void should_explain_that_optional_members_must_be_explicit() => _diagnostics.Single().Message.ShouldContain("including optional members");
}
