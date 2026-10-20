// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.given;

/// <summary>
/// A compiled model whose single specification is validated and rendered on its own, whatever the kind of slice that declares it.
/// </summary>
public class a_specification_in_a_slice : Specification
{
    private protected SemanticApplicationContext _context = null!;
    private protected LocatedSemanticSlice _located = null!;
    private protected SemanticSpecification _specification = null!;
    private protected List<ArtifactRenderDiagnostic> _diagnostics = [];
    private protected string _source = string.Empty;

    protected void Prepare(string source)
    {
        var model = for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(source);
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Reactions", new("Reactions", "Reactions")), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _context = new(request, new("Reactions", "Reactions"));
        _located = _context.SelectedSlices().Single(slice => !slice.Slice.Specifications.IsDefaultOrEmpty);
        _specification = _located.Slice.Specifications.Single();
    }

    protected void Validate() => SemanticSpecificationAdmission.Validate(_context, _located.Slice with { Specifications = [_specification] }, _diagnostics);

    protected void Render() => _source = SemanticSpecificationArtifactRenderer.Render(_specification, _context).Single().Content;
}
