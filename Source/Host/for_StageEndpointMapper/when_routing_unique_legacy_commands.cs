// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_routing_unique_legacy_commands : given.a_routed_model
{
    readonly List<int> _statuses = [];

    async Task Because()
    {
        MapModel(RouteModels.Model(RouteModels.Command("PlaceOrder", "Place"), RouteModels.Command("CancelOrder", "Cancel")));
        foreach (var route in new[] { "place", "cancel", "place/validate", "cancel/validate" })
        {
            _statuses.Add((await Request("POST", $"/api/orders/checkout/{route}")).Status);
        }
    }

    [Fact] void should_keep_unique_legacy_execution_and_validation_aliases() => _statuses.ToArray().ShouldEqual([200, 200, 200, 200]);
    [Fact] void should_bind_legacy_place_to_the_same_canonical_type() => _commands[0].Type.FullName.ShouldEqual("Stage.Orders.Checkout.PlaceOrder.Place");
    [Fact] void should_bind_legacy_cancel_to_the_same_canonical_type() => _commands[1].Type.FullName.ShouldEqual("Stage.Orders.Checkout.CancelOrder.Cancel");
    [Fact] void should_validate_place_without_appending() => _commands[2].Type.ShouldEqual(_commands[0].Type);
    [Fact] void should_validate_cancel_without_appending() => _commands[3].Type.ShouldEqual(_commands[1].Type);
    [Fact] void should_append_only_twice() => _appends.Count.ShouldEqual(2);
}
