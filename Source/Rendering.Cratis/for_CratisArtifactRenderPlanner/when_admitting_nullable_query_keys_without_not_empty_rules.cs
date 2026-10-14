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

public class when_admitting_nullable_query_keys_without_not_empty_rules : Specification
{
    IReadOnlyList<ArtifactRenderDiagnostic> _diagnostics = [];

    void Because()
    {
        var model = invoice_model.Compile(a_validated_query_key_model.Source);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var located = context.SelectedSlices().Single(value => value.Slice.Queries.Length > 0);
        var slice = located.Slice with
        {
            Queries = [.. located.Slice.Queries.Select(query => query with { Argument = query.Argument! with { Type = query.Argument!.Type with { IsOptional = true } } })]
        };
        _diagnostics = [.. SemanticCratisAdmission.Evaluate(context, [located with { Slice = slice }])];
    }

    [Fact] void should_not_report_the_null_argument_validation_gap() => _diagnostics.Any(diagnostic => diagnostic.Message.Contains("Arc skips validation for null arguments", StringComparison.Ordinal)).ShouldBeFalse();
}
