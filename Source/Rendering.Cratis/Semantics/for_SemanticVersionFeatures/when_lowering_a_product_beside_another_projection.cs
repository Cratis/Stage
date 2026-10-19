// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

// Lowering one projection to a reducer must leave every other projection rendered byte for byte as before.
public class when_lowering_a_product_beside_another_projection : Specification
{
    const string NamesPath = "Shop/Baskets/BasketNames/BasketNames.cs";
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _withoutProduct = null!;
    string _basket = string.Empty;

    void Establish()
    {
        var withoutProduct = a_product_model.Source.Replace("NAME_TYPE", "String?", StringComparison.Ordinal)
            .Replace("          add total by unitPrice * quantity", "          add total by unitPrice", StringComparison.Ordinal);
        _withoutProduct = a_product_model.Plan(withoutProduct);
    }

    void Because()
    {
        _plan = a_product_model.Planned(a_product_model.Source.Replace("NAME_TYPE", "String?", StringComparison.Ordinal));
        _basket = a_product_model.Artifact(_plan, "Shop/Baskets/BasketLookup/BasketLookup.cs");
    }

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_plan_the_application_without_the_product() => _withoutProduct.Success.ShouldBeTrue();
    [Fact] void should_render_the_product_projection_as_a_reducer() => _basket.ShouldContain("public class BasketProjection : global::Cratis.Chronicle.Reducers.IReducerFor<Basket>");
    [Fact] void should_render_the_other_projection_unchanged() => a_product_model.Artifact(_plan, NamesPath).ShouldEqual(a_product_model.Artifact(_withoutProduct, NamesPath));
    [Fact] void should_still_render_a_projection_without_a_product() => a_product_model.Artifact(_withoutProduct, "Shop/Baskets/BasketLookup/BasketLookup.cs").ShouldContain("IProjectionFor<Basket>");
}
