// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_routing_nested_features_in_distinct_modules : given.a_routed_model
{
    readonly List<int> _statuses = [];

    async Task Because()
    {
        var model = RouteModels.WithFeatures(RouteModels.Feature(
            "Checkout",
            [],
            RouteModels.Feature("Packing", [RouteModels.Command("PlaceOrder"), RouteModels.Query("OrderReport")])));
        var collection = model.Collections[0];
        model = model with
        {
            Collections = [collection with { Modules = [collection.Modules[0], collection.Modules[0] with { Name = "Billing" }] }]
        };
        MapModel(model);
        foreach (var module in new[] { "orders", "billing" })
        {
            _statuses.Add((await Request("POST", $"/api/{module}/checkout/packing/place-order/do-it")).Status);
            _statuses.Add((await Request("POST", $"/api/{module}/checkout/packing")).Status);
            _statuses.Add((await Request("GET", $"/api/{module}/checkout/packing/order-report/all-orders")).Status);
        }
    }

    [Fact] void should_select_canonical_and_singleton_aliases_in_each_module() => _statuses.SequenceEqual([200, 200, 200, 200, 200, 200]).ShouldBeTrue();
    [Fact] void should_keep_the_nested_orders_identity() => _appends[0].BoundType.FullName.ShouldEqual("Stage.Orders.Checkout.Packing.PlaceOrder.DoIt");
    [Fact] void should_keep_the_nested_billing_identity() => _appends[2].BoundType.FullName.ShouldEqual("Stage.Billing.Checkout.Packing.PlaceOrder.DoIt");
    [Fact] void should_keep_query_identities_distinct_across_modules() => _queries.Select(context => context.Name).Distinct().Count().ShouldEqual(2);
}
