// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_routing_unique_legacy_queries : given.a_routed_model
{
    readonly List<int> _statuses = [];

    async Task Because()
    {
        MapModel(RouteModels.Model(RouteModels.Query("OrderReport"), RouteModels.Query("InvoiceReport", "Invoice")));
        foreach (var method in new[] { "GET", "QUERY" })
        {
            foreach (var name in new[] { "get-order-by-id", "all-orders", "get-invoice-by-id", "all-invoices" })
            {
                _statuses.Add((await Request(method, $"/api/orders/checkout/{name}?id=11111111-1111-1111-1111-111111111111", """{"arguments":{"id":"11111111-1111-1111-1111-111111111111"}}""")).Status);
            }
        }
    }

    [Fact] void should_route_every_unique_legacy_query_without_changing_authorization() => (_statuses.Count == 8 && _statuses.TrueForAll(status => status == 403)).ShouldBeTrue();
    [Fact] void should_route_each_identity_twice() => (_queries.Count == 8 && _queries.GroupBy(context => context.Name).All(group => group.Count() == 2)).ShouldBeTrue();
    [Fact] void should_keep_the_original_qualified_query_names() => _queries.Select(context => context.Name.ToString()).Distinct().Order(StringComparer.Ordinal).SequenceEqual(["Stage.Orders.Checkout.InvoiceReport.Invoice.AllInvoices", "Stage.Orders.Checkout.InvoiceReport.Invoice.GetInvoiceById", "Stage.Orders.Checkout.OrderReport.Order.AllOrders", "Stage.Orders.Checkout.OrderReport.Order.GetOrderById"]).ShouldBeTrue();
}
