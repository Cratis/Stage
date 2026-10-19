// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

// The reducer keeps the reference arithmetic: zero for a missing target, an exact decimal product, half-to-even rounding for a
// whole target, and deletion for a removal.
public class when_rendering_a_product_reducer : Specification
{
    const string Source = """
        concept BasketId : Uuid
        concept Money : Decimal
        module Shop
          feature Baskets
            slice StateChange AddLine
              command AddLine
                basketId BasketId identifier
                unitPrice Money
                quantity Int
                discount Decimal
                produces LineAdded
                  for basketId
                  unitPrice = unitPrice
                  quantity = quantity
                  discount = discount
              event LineAdded
                unitPrice Money
                quantity Int
                discount Decimal
            slice StateChange ClearBasket
              command ClearBasket
                basketId BasketId identifier
                produces BasketCleared
                  for basketId
              event BasketCleared
            slice StateView BasketLookup
              readmodel Basket
                basketId BasketId key
                total Money?
                units Int
                rounded Int?
                saved Decimal?
              query BasketById => Basket optional
                by basketId BasketId
              projection BasketProjection => Basket
                from LineAdded
                  add total by unitPrice * quantity
                  add units by quantity
                  add rounded by unitPrice * quantity
                  subtract saved by discount * quantity
                remove with BasketCleared
        """;

    ArtifactRenderPlan _plan = null!;
    string _basket = string.Empty;
    RenderedFile[] _sources = [];

    void Because()
    {
        _plan = a_product_model.Planned(Source);
        _basket = a_product_model.Artifact(_plan, "Shop/Baskets/BasketLookup/BasketLookup.cs");
        _sources = [.. _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
    }

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_seed_the_identifier_from_the_event_source() =>
        _basket.ShouldContain("BasketId: new global::Shop.Common.BasketId(global::System.Guid.Parse(eventContext.EventSourceId.Value))");
    [Fact] void should_add_the_exact_product_to_a_missing_total_as_zero() =>
        _basket.ShouldContain("state = state with { Total = new global::Shop.Common.Money(((decimal)(state.Total?.Value ?? 0) + (decimal)@event.UnitPrice.Value * (decimal)@event.Quantity)) };");
    [Fact] void should_round_a_whole_target_half_to_even() =>
        _basket.ShouldContain("state = state with { Rounded = (long)((decimal)(state.Rounded ?? 0) + global::System.Math.Round((decimal)@event.UnitPrice.Value * (decimal)@event.Quantity, global::System.MidpointRounding.ToEven)) };");
    [Fact] void should_subtract_a_product_from_a_missing_total_as_zero() =>
        _basket.ShouldContain("state = state with { Saved = ((decimal)(state.Saved ?? 0) - (decimal)@event.Discount * (decimal)@event.Quantity) };");
    [Fact] void should_delete_on_removal() => _basket.ShouldContain("return null;");
    [Fact] void should_compile() => RenderedOutput.Load(_sources).ShouldNotBeNull();
}
