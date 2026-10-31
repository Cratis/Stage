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

public class when_describing_whether_a_command_creates_its_entity : Specification
{
    StageRoutes _routes = null!;

    async Task Because()
    {
        var plan = compiled_plan.From(Source);
        await using var app = WebApplication.CreateBuilder().Build();
        var scene = new SceneApplication([], [], [], [], [], []);
        _routes = StageRoutes.For(new StageSceneRoutes(scene, app.Services, NullLogger.Instance), plan.Model);
    }

    bool Creates(string command)
    {
        using var schema = JsonDocument.Parse(_routes.CommandSchemas[command]);
        return schema.RootElement.TryGetProperty("x-creates", out var mark) && mark.GetBoolean();
    }

    [Fact] void should_mark_a_command_whose_identifier_is_generated() => Creates("PlanShipment").ShouldBeTrue();
    [Fact] void should_mark_a_command_without_an_identifier() => Creates("NoteDelivery").ShouldBeTrue();
    [Fact] void should_not_mark_a_command_whose_identifier_the_caller_supplies() => Creates("DispatchShipment").ShouldBeFalse();

    const string Source = """
        concept OrderId : Uuid
        concept ShipmentId : Uuid
        module Stock
          feature Dispatch
            slice StateChange PlanShipment
              command PlanShipment
                shipmentId ShipmentId generated identifier
                orderId    OrderId
                produces event ShipmentPlanned
                  orderId OrderId = orderId
            slice StateChange NoteDelivery
              command NoteDelivery
                orderId OrderId
                note    String
                returns note
            slice StateChange DispatchShipment
              command DispatchShipment
                orderId    OrderId identifier
                carrier    String
                shipmentId ShipmentId generated
                produces event ShipmentHandedToCarrier
                  shipmentId ShipmentId = shipmentId
                  carrier    String = carrier
        """;
}
