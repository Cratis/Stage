// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_commands_execute_through_the_harness : given.a_routed_model
{
    const string Marker = "Stage.Spec.Marker";

    async Task Because()
    {
        MapModel(RouteModels.Model(RouteModels.Command("PlaceOrder", "Place"), RouteModels.Command("CancelOrder", "Cancel")));
        await Request("POST", "/api/orders/checkout/place");
        _commands[0].Values[Marker] = "first";
        await Request("POST", "/api/orders/checkout/cancel");
    }

    [Fact] void should_execute_both_commands() => _commands.Count.ShouldEqual(2);
    [Fact] void should_give_each_command_its_own_values() => _commands[1].Values.ShouldNotBeSame(_commands[0].Values);
    [Fact] void should_not_leak_values_written_by_the_first_command() => _commands[1].Values.ContainsKey(Marker).ShouldBeFalse();
}
