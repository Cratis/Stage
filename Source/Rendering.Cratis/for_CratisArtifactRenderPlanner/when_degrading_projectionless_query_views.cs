// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_degrading_projectionless_query_views : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(SemanticModelSerializer.Deserialize(QueryValidationCorpus.V18.EsmBytes.AsSpan()));

    [Fact] void should_degrade_the_unbuilt_read_model_even_for_empty_query_specifications() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-DEGRADE-002");
    [Fact] void should_not_refuse_the_unbuilt_read_model() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-007");
    [Fact] void should_keep_the_plan_successful() => _plan.Success.ShouldBeTrue();
}
