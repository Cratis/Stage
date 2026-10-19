// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

// A line can create the basket before it is named; the reference refuses the unnamed basket, a reducer would store it.
public class when_refusing_projection_products_outside_the_reducer_subset : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = a_product_model.Plan(a_product_model.Source.Replace("NAME_TYPE", "String", StringComparison.Ordinal));

    [Fact] void should_not_plan_the_application() => _plan.Success.ShouldBeFalse();
    [Fact] void should_refuse_the_product() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-050");
    [Fact] void should_name_the_projection() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-050").Message.ShouldContain("BasketProjection");
}
