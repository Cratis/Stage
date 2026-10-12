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

public class when_rejecting_nested_validated_query_keys : Specification
{
    ArtifactRenderDiagnostic[][] _diagnostics = [];

    void Because()
    {
        var model = invoice_model.Compile("type InvoiceKey\n  invoiceId InvoiceId\n" + a_validated_query_key_model.Source);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var slice = context.SelectedSlices().Single(value => value.Slice.Queries.Length > 0);
        var type = model.Application.Types.Single();
        _diagnostics = [Evaluate(context, slice, argument => argument with { IsCollection = true }),
            Evaluate(context, slice, argument => argument with { Kind = SemanticTypeReferenceKind.CompositeType, Primitive = SemanticPrimitiveType.Unknown, Target = type.Id })];
    }

    [Fact] void should_refuse_each_unsupported_argument_shape() => Assert.All(_diagnostics, diagnostics => diagnostics.ShouldNotBeEmpty());
    [Fact] void should_name_the_query_and_validated_concept() => Assert.All(_diagnostics, diagnostics => Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-010" && diagnostic.Message.Contains("InvoicesById", StringComparison.Ordinal) && diagnostic.Message.Contains("InvoiceId", StringComparison.Ordinal)));

    static ArtifactRenderDiagnostic[] Evaluate(SemanticApplicationContext context, LocatedSemanticSlice located, Func<SemanticTypeReference, SemanticTypeReference> change)
    {
        // Current ESM contracts only admit scalar query keys. Check the renderer's own boundary
        // independently so future composite/collection keys cannot silently gain Arc validation.
        var slice = located.Slice with
        {
            Queries = [.. located.Slice.Queries.Select(query => query.Name == "InvoicesById"
                ? query with { Argument = query.Argument! with { Type = change(query.Argument!.Type) } } : query)]
        };
        return [.. SemanticCratisAdmission.Evaluate(context, [located with { Slice = slice }])];
    }
}
