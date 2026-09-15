// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_query_transport_is_disabled : given.a_routed_model
{
    readonly List<int> _getStatuses = [];
    readonly List<int> _queryStatuses = [];

    async Task Because()
    {
        MapModel(RouteModels.Model(RouteModels.Query("OrderReport")), enableQueryHttpMethod: false);
        foreach (var path in new[] { "/api/orders/checkout/order-report/all-orders", "/api/orders/checkout/all-orders" })
        {
            _getStatuses.Add((await Request("GET", path)).Status);
            _queryStatuses.Add((await Request("QUERY", path)).Status);
        }
    }

    [Fact] void should_keep_canonical_and_legacy_get_routes_with_sandbox_authorization() => _getStatuses.SequenceEqual([200, 200]).ShouldBeTrue();
    [Fact] void should_not_allow_query_on_canonical_or_legacy_paths() => _queryStatuses.SequenceEqual([405, 405]).ShouldBeTrue();
    [Fact] void should_not_plan_an_alternate_query_transport() => _surface.Operations.Any(operation => operation.Method == "QUERY").ShouldBeFalse();
    [Fact] void should_not_map_query_endpoints() => Endpoints().Any(endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains("QUERY")).ShouldBeFalse();
    [Fact] void should_invoke_only_the_two_get_operations() => _queries.Count.ShouldEqual(2);
}
