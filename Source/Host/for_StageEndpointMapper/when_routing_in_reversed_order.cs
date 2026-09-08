// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_routing_in_reversed_order : given.a_routed_model
{
    readonly List<int> _statuses = [];

    async Task Because()
    {
        MapModel(RouteModels.Reverse(EventModelLoader.LoadFromSource(RouteModels.SameNamedCommands)));
        _statuses.Add((await Request("POST", "/api/orders/checkout/place-order/do-it")).Status);
        _statuses.Add((await Request("POST", "/api/orders/checkout/cancel-order/do-it")).Status);
        _statuses.Add((await Request("POST", "/api/orders/checkout/do-it")).Status);
        _statuses.Add((await Request("POST", "/api/orders/checkout/do-it/validate")).Status);
    }

    [Fact] void should_make_the_same_alias_decisions() => _statuses.ToArray().ShouldEqual([200, 200, 404, 404]);
    [Fact] void should_still_dispatch_place_order_first() => _appends[0].BoundType.FullName.ShouldEqual("Stage.Orders.Checkout.PlaceOrder.DoIt");
    [Fact] void should_still_dispatch_cancel_order_second() => _appends[1].BoundType.FullName.ShouldEqual("Stage.Orders.Checkout.CancelOrder.DoIt");
}
