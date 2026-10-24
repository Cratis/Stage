// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.when_visiting;

public class and_plain_actions_share_a_screen_with_a_table : Specification
{
    SceneApplication _scene = null!;

    void Because()
    {
        var parsed = new ScreenplayCompiler().Parse("""
            concept ShipmentId : Uuid
            module Shipping
              feature Dispatch
                slice StateChange DispatchShipment
                  command DispatchShipment
                    shipmentId ShipmentId identifier
                    carrier String
                  screen Other
                    action DispatchShipment
                slice StateChange CreateShipment
                  command CreateShipment
                    shipmentId ShipmentId generated identifier
                    carrier String
                slice StateView Shipments
                  readmodel ShipmentRow
                    shipmentId ShipmentId
                    carrier String
                  query ListShipments => ShipmentRow[]
                  screen Shipments
                    data ShipmentRow[] via query ListShipments
                    action DispatchShipment
                    action CreateShipment
                    table ShipmentRow
                      column carrier
            """);
        Assert.True(parsed.Success, string.Join(Environment.NewLine, parsed.Diagnostics));
        _scene = new ScreenplaySceneVisitor().Visit(parsed.Value!);
    }

    [Fact] void should_bind_the_plain_action_to_the_table_selection() => Action("Shipments", "DispatchShipment").Properties["rowAction"].ShouldEqual(true);
    [Fact] void should_not_bind_a_generated_identifier() => Action("Shipments", "CreateShipment").Properties.ContainsKey("rowAction").ShouldBeFalse();
    [Fact] void should_not_use_a_table_from_another_screen() => Action("Other", "DispatchShipment").Properties.ContainsKey("rowAction").ShouldBeFalse();

    ExternalComponent Action(string screen, string command) => _scene.Screens.Single(candidate => candidate.Name == screen).SlotContent.Values
        .SelectMany(elements => elements).OfType<ExternalComponent>().Single(element => element.ComponentName == "core:action" && Equals(element.Properties["command"], command));
}
