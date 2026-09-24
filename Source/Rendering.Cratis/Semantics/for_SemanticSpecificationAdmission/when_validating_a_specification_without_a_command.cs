// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;

public class when_validating_a_specification_without_a_command : Specification
{
    readonly List<ArtifactRenderDiagnostic> _diagnostics = [];
    SemanticApplicationContext _context = null!;
    SemanticSlice _slice = null!;
    Exception? _error;

    void Establish()
    {
        var model = invoice_model.Compile(invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource));
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        _context = new(
            new(
                model,
                SemanticExecutionPlan.Compile(model).Plan!,
                CratisRendering.CreateProfile(model.Application.Name, options),
                new(ArtifactRenderScopeKind.Application, model.Application.Id)),
            options);
        var slice = _context.SelectedSlices().Single(_ => _.Slice.Kind == SemanticSliceKind.StateChange).Slice;

        // A whenless specification establishes facts and inspects state without executing a command.
        _slice = slice with { Specifications = [slice.Specifications[0] with { When = null }] };
    }

    void Because() => _error = Catch.Exception(() => SemanticSpecificationAdmission.Validate(_context, _slice, _diagnostics));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_reject_the_specification() => _diagnostics.Select(_ => _.Code).ShouldContainOnly(["STAGE-ESM-011"]);
}
