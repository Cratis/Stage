// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

// ESM v29 'add total by unitPrice * quantity': the renderer lowers the product to a member of the event (see for_SemanticProjectionProducts),
// so no product is refused (the corpus vector stays refused for its Translate slice and public events, which are not products).
public class when_refusing_projection_products : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;
    SemanticVersionFeature[] _features = [];

    void Establish() => _model = SemanticModelSerializer.Deserialize(AcmeAdmissionsCorpus.V29.EsmBytes.AsSpan());

    void Because()
    {
        var request = new ArtifactRenderRequest(
            _model,
            SemanticExecutionPlan.Compile(_model, []).Plan!,
            CratisRendering.CreateProfile("Acme", new("Acme", "Acme")),
            new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Acme", "Acme"));
        _features = [.. SemanticVersionFeatures.Slices(_model.Application).SelectMany(slice => SemanticVersionFeatures.ProjectionProductsInSlice(slice, context))];
        _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Acme", "Acme"));
    }

    [Fact] void should_refuse_no_product_the_renderer_can_lower() => _features.ShouldBeEmpty();
    [Fact] void should_not_report_a_product_refusal_in_the_plan() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-050");
}
