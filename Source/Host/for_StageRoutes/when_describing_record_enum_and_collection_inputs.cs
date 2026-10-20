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

public class when_describing_record_enum_and_collection_inputs : Specification
{
    JsonElement _schema;

    async Task Because()
    {
        var plan = compiled_plan.From(Source);
        await using var app = WebApplication.CreateBuilder().Build();
        var scene = new SceneApplication([], [], [], [], [], []);
        var routes = StageRoutes.For(new StageSceneRoutes(scene, app.Services, NullLogger.Instance), plan.Model);
        using var schema = JsonDocument.Parse(routes.CommandSchemas["CheckOut"]);
        _schema = schema.RootElement.Clone();
    }

    JsonElement Property(string name) => _schema.GetProperty("properties").GetProperty(name);

    [Fact] void should_describe_an_enumeration_by_its_declared_values() => Property("method").GetProperty("enum").EnumerateArray().Select(value => value.GetString()).ShouldContainOnly("standard", "express", "pickup");
    [Fact] void should_describe_a_record_by_its_members() => Property("shipTo").GetProperty("properties").EnumerateObject().Select(member => member.Name).ShouldContainOnly("street", "city", "country", "kind");
    [Fact] void should_require_the_members_that_are_not_optional() => Property("shipTo").GetProperty("required").EnumerateArray().Select(member => member.GetString()).ShouldContainOnly("street", "city");
    [Fact] void should_describe_a_collection_of_records_by_the_members_of_its_elements() => Property("lines").GetProperty("items").GetProperty("properties").EnumerateObject().Select(member => member.Name).ShouldContainOnly("sku", "quantity");
    [Fact] void should_describe_a_collection_as_an_array() => Property("lines").GetProperty("type").GetString().ShouldEqual("array");
    [Fact] void should_describe_an_enumeration_within_a_record() => Property("shipTo").GetProperty("properties").GetProperty("kind").GetProperty("enum").EnumerateArray().Select(value => value.GetString()).ShouldContainOnly("home", "work");

    const string Source = """
        concept OrderId : Uuid
        concept DeliveryMethod : Enum
          standard
          express
          pickup
        concept AddressKind : Enum
          home
          work
        type PostalAddress
          street  String
          city    String
          country String optional
          kind    AddressKind optional
        type OrderLine
          sku      String
          quantity Int
        module Shop
          feature Checkout
            slice StateChange CheckOut
              command CheckOut
                orderId  OrderId generated identifier
                lines    OrderLine[]
                shipTo   PostalAddress
                method   DeliveryMethod
                produces event CheckedOut
                  method DeliveryMethod = method
        """;
}
