// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.given;

/// <summary>
/// A compiled application with queries a performer answers, validated and rendered through the state-view renderer.
/// </summary>
public class a_performed_query_application : Specification
{
    internal const string Source =
        """
        policy Buyers
          require claim "role" matches "buyer"
        module Billing
          authorize Buyers
          feature Invoices
            slice StateView InvoiceFigures
              readmodel InvoiceFigures
                invoiceId String
                region String
                status String
                total Int
              query CurrentFigures => InvoiceFigures
                by invoiceId String
                performer
                  file Queries/CurrentFigures.cs
              query FiguresInRegion => InvoiceFigures[]
                filter region String
                filter status String optional
                performer
                  file Queries/FiguresInRegion.cs
              query LiveFigures => observable InvoiceFigures[]
                performer
                  file Queries/LiveFigures.cs
        """;

    private protected SemanticApplicationContext _context = null!;
    private protected ArtifactRenderDiagnostic[] _diagnostics = [];
    private protected string _source = string.Empty;

    protected void Prepare(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("billing"), "billing", "Billing.play", source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var options = new CratisRenderingOptions("Billing", "Billing");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Billing", options), new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = compilation.ImplementationRequirements,
            ImplementationContents = []
        };
        _context = new(request, options);
    }

    protected void Evaluate()
    {
        var slices = _context.SelectedSlices();
        _diagnostics = [.. SemanticCratisAdmission.Evaluate(_context, slices)];
    }

    protected void Render() => _source = SemanticStateViewArtifactRenderer.Render(_context.SelectedSlices().Single(), _context).Content;
}
