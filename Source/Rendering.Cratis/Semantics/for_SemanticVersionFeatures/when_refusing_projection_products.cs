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

// ESM v29 'add total by unitPrice * quantity': Chronicle adds one event property, so the renderer refuses it instead of rendering a sum of one factor.
public class when_refusing_projection_products : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;
    SemanticVersionFeature[] _features = [];

    void Establish() => _model = SemanticModelSerializer.Deserialize(AcmeAdmissionsCorpus.V29.EsmBytes.AsSpan());

    void Because()
    {
        _features = [.. SemanticVersionFeatures.Slices(_model.Application).SelectMany(SemanticVersionFeatures.ProjectionProductsInSlice)];
        _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Acme", "Acme"));
    }

    [Fact] void should_find_the_one_projection_that_multiplies() => _features.Length.ShouldEqual(1);
    [Fact] void should_refuse_it_with_its_own_code() => _features.Single().Code.ShouldEqual("STAGE-ESM-050");
    [Fact] void should_name_the_projection() => _features.Single().Message.ShouldContain("BasketProjection");
    [Fact] void should_not_plan_the_application() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_the_refusal_in_the_plan() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-050");
}
