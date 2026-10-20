// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cratis.Stage.Host.for_StageRoutes;

public class when_describing_the_identity_a_command_acts_on : Specification
{
    JsonElement _addToBasket;

    async Task Because()
    {
        var plan = compiled_plan.From(Source);
        await using var app = WebApplication.CreateBuilder().Build();
        var scene = new SceneApplication([], [], [], [], [], []);
        var routes = StageRoutes.For(new StageSceneRoutes(scene, app.Services, NullLogger.Instance), plan.Model);
        using var addToBasket = JsonDocument.Parse(routes.CommandSchemas["AddToBasket"]);
        _addToBasket = addToBasket.RootElement.Clone();
    }

    static JsonElement Property(JsonElement schema, string name) => schema.GetProperty("properties").GetProperty(name);

    [Fact] void should_mark_the_identifier_the_command_acts_on() => Property(_addToBasket, "basketId").GetProperty("x-identity").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_the_concept_of_the_identifier() => Property(_addToBasket, "basketId").GetProperty("x-concept").GetString().ShouldEqual("BasketId");
    [Fact] void should_not_mark_a_reference_to_another_entity() => Property(_addToBasket, "productId").TryGetProperty("x-identity", out _).ShouldBeFalse();
    [Fact] void should_keep_the_concept_of_the_reference() => Property(_addToBasket, "productId").GetProperty("x-concept").GetString().ShouldEqual("ProductId");
    [Fact] void should_not_mark_a_plain_value() => Property(_addToBasket, "quantity").TryGetProperty("x-identity", out _).ShouldBeFalse();

    const string Source = """
        concept BasketId : Uuid
        concept ProductId : Uuid
        module Shop
          feature Basket
            slice StateChange AddToBasket
              command AddToBasket
                basketId  BasketId identifier
                productId ProductId
                quantity  Int
                produces event ProductAddedToBasket
                  productId ProductId = productId
                  quantity  Int = quantity
        """;
}
