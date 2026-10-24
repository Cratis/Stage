// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StageSceneDocument;

public class when_plain_actions_share_a_screen_with_a_table : Specification
{
    JsonNode _screens = null!;

    void Because()
    {
        var model = invoice_model.Compile("""
            concept ShipmentId : Uuid
            module Shipping
              feature Dispatch
                slice StateChange DispatchShipment
                  command DispatchShipment
                    shipmentId ShipmentId identifier
                    carrier String
                    produces ShipmentDispatched
                      for shipmentId
                      carrier = carrier
                  event ShipmentDispatched
                    carrier String
                slice StateChange CreateShipment
                  command CreateShipment
                    shipmentId ShipmentId generated identifier
                    carrier String
                    produces ShipmentCreated
                      for shipmentId
                      carrier = carrier
                  event ShipmentCreated
                    carrier String
                slice StateView Shipments
                  readmodel ShipmentRow
                    shipmentId ShipmentId
                    carrier String
                  query AllShipments => ShipmentRow[]
            """);
        var profile = CratisRendering.CreateProfile("Shipping", new("Shipping", "Shipping"));
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Shipping", "Shipping"));
        _screens = JsonNode.Parse(StageSceneDocument.Render(
            """
            {"screens":[
              {"name":"Shipments","slotContent":{"content":[
                {"componentName":"core:action","properties":{"command":"DispatchShipment"}},
                {"componentName":"core:action","properties":{"command":"CreateShipment"}},
                {"componentName":"core:table","properties":{"typeName":"ShipmentRow"}}
              ]}},
              {"name":"Other","slotContent":{"content":[
                {"componentName":"core:action","properties":{"command":"DispatchShipment"}}
              ]}}
            ]}
            """,
            context))!["screens"]!;
    }

    [Fact] void should_mark_the_plain_action_as_a_row_action() => Action(0, 0)["rowAction"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_hide_the_carried_identifier() => Fields(0, 0).ShouldContainOnly("carrier");
    [Fact] void should_keep_the_identifier_in_the_submission_schema() => Action(0, 0)["schema"]!.GetValue<string>().ShouldContain("shipmentId");
    [Fact] void should_not_bind_a_generated_identifier() => Action(0, 1).ContainsKey("rowAction").ShouldBeFalse();
    [Fact] void should_not_ask_for_the_generated_identifier() => Fields(0, 1).ShouldContainOnly("carrier");
    [Fact] void should_not_use_another_screens_table() => Action(1, 0).ContainsKey("rowAction").ShouldBeFalse();
    [Fact] void should_keep_the_identifier_field_on_a_screen_without_a_table() => Fields(1, 0).ShouldContain("shipmentId");

    JsonObject Action(int screen, int action) => _screens[screen]!["slotContent"]!["content"]![action]!["properties"]!.AsObject();
    string[] Fields(int screen, int action) => [.. Action(screen, action)["fields"]!.AsArray().Select(field => field!["name"]!.GetValue<string>())];
}
