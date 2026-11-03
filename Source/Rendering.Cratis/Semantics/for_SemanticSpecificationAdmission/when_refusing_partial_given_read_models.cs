// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;

public class when_refusing_partial_given_read_models : Specification
{
    readonly List<ArtifactRenderDiagnostic> _diagnostics = [];
    SemanticApplicationContext _context = null!;
    SemanticSlice _slice = null!;

    void Establish()
    {
        var model = invoice_model.Compile(when_rendering_projected_read_models_with_given_read_models.Source);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        _context = new(
            new(
                model,
                SemanticExecutionPlan.Compile(model).Plan!,
                CratisRendering.CreateProfile(model.Application.Name, options),
                new(ArtifactRenderScopeKind.Application, model.Application.Id)),
            options);
        var slice = _context.SelectedSlices().Single(selected => selected.Slice.Kind == SemanticSliceKind.StateChange).Slice;
        var specification = slice.Specifications.Single();
        var seed = specification.GivenReadModels.Single();
        _slice = slice with
        {
            Specifications = [specification with { GivenReadModels = [seed with { Values = [seed.Values[0]] }] }]
        };
    }

    void Because() => SemanticSpecificationAdmission.Validate(_context, _slice, _diagnostics);

    [Fact] void should_refuse_partial_projected_state() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_explain_the_remaining_seed_limitations() => _diagnostics.Single().Message.ShouldEqual("Specification 'CommandUpdatesSeed' cannot render: Given read-model state requires complete non-null scalar records with identifiers matching their keys and independent projections. Seeded command reads, reducers and query combinations are not supported; a query-only lookup of one complete seeded record is supported.");
}
#endif
