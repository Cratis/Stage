// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.given;

/// <summary>
/// A compiled invoice model whose chosen specification is mutated and then validated on its own.
/// </summary>
public class an_invoice_specification : Specification
{
    private protected SemanticApplicationContext _context = null!;
    private protected SemanticSlice _slice = null!;
    private protected SemanticSpecification _specification = null!;
    private protected List<ArtifactRenderDiagnostic> _diagnostics = [];

    protected void Prepare(string source, string name)
    {
        var model = invoice_model.Compile(source);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        _context = new(
            new(
                model,
                SemanticExecutionPlan.Compile(model).Plan,
                CratisRendering.CreateProfile(model.Application.Name, options),
                new(ArtifactRenderScopeKind.Application, model.Application.Id)),
            options);
        _slice = _context.SelectedSlices().Single(selected => selected.Slice.Specifications.Any(specification => specification.Name == name)).Slice;
        _specification = _slice.Specifications.Single(specification => specification.Name == name);
    }

    protected void Validate() => SemanticSpecificationAdmission.Validate(_context, _slice with { Specifications = [_specification] }, _diagnostics);

    protected string Messages() => string.Join(" | ", _diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
}
