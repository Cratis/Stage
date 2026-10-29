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

public class when_admitting_nested_validated_query_keys : Specification
{
    ArtifactRenderDiagnostic[][] _diagnostics = [];
    readonly List<string> _sources = [];

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

    [Fact] void should_admit_each_validated_argument_shape() => Assert.All(_diagnostics, diagnostics => diagnostics.Where(diagnostic => diagnostic.Code != "STAGE-ESM-054" || diagnostic.Severity != ArtifactRenderDiagnosticSeverity.Information).ShouldBeEmpty());
    [Fact] void should_preserve_the_concept_collection_parameter() => _sources[0].ShouldContain("IReadOnlyList<InvoiceId> invoiceId");
    [Fact] void should_preserve_the_composite_parameter() => _sources[1].ShouldContain("InvoiceKey invoiceId");

    ArtifactRenderDiagnostic[] Evaluate(SemanticApplicationContext context, LocatedSemanticSlice located, Func<SemanticTypeReference, SemanticTypeReference> change)
    {
        // Check the renderer boundary independently of the compiler's argument-shape admission.
        var slice = located.Slice with
        {
            Queries = [.. located.Slice.Queries.Select(query => query.Name == "InvoicesById"
                ? query with { Argument = query.Argument! with { Type = change(query.Argument!.Type) } } : query)]
        };
        var candidate = located with { Slice = slice };
        _sources.Add(SemanticStateViewArtifactRenderer.Render(candidate, context).Content);
        return [.. SemanticCratisAdmission.Evaluate(context, [candidate])];
    }
}
