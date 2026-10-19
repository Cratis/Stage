// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

// The reducer declares methods named On, and C# forbids a member named like its enclosing type.
public class when_refusing_a_product_reducer_named_on : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = a_product_model.Plan(a_product_model.Source
        .Replace("NAME_TYPE", "String?", StringComparison.Ordinal)
        .Replace("projection BasketProjection", "projection On", StringComparison.Ordinal));

    [Fact] void should_not_plan_the_application() => _plan.Success.ShouldBeFalse();
    [Fact] void should_refuse_the_name() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-012").Message.ShouldContain("'On'");
}
