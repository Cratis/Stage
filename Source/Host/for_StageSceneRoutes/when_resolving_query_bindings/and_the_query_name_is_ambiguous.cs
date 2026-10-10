// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageSceneRoutes.when_resolving_query_bindings;

public class and_the_query_name_is_ambiguous : given.registered_endpoints
{
    IReadOnlyDictionary<string, string> _routes = null!;

    void Establish()
    {
        Query("Orders.Checkout.Current.OrderSummary.OpenOrders", "/api/orders/checkout/current/open-orders", "/api/orders/checkout/open-orders-current");
        Query("Orders.Archive.Old.ArchivedOrder.OpenOrders", "/api/orders/archive/old/open-orders", "/api/orders/archive/open-orders");
    }

    void Because()
    {
        Resolve(Data("orders", "OpenOrders"), Data("current", "OpenOrders", "OrderSummary"));
        _routes = StageSceneRoutes.RoutesByName(Endpoints(), "GET");
    }

    [Fact] void should_not_attach_a_route() => Properties("orders").ContainsKey("route").ShouldBeFalse();
    [Fact] void should_mark_the_binding_ambiguous() => Properties("orders")[StageSceneRoutes.RouteStatusProperty].ShouldEqual("ambiguous");
    [Fact] void should_name_both_candidates() => ((string)Properties("orders")[StageSceneRoutes.RouteDiagnosticProperty]!).ShouldContain("Orders.Archive.Old.ArchivedOrder.OpenOrders, Orders.Checkout.Current.OrderSummary.OpenOrders");
    [Fact] void should_resolve_the_binding_its_read_model_disambiguates() => Properties("current")["route"].ShouldEqual("/api/orders/checkout/current/open-orders");
    [Fact] void should_leave_the_ambiguous_name_out_of_the_route_lookup() => _routes.ContainsKey("OpenOrders").ShouldBeFalse();
}
