// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_arc_maps_the_surface_again : given.a_routed_model
{
    string[] _before = [];
    string[] _after = [];
    int _commandStatus;
    int _queryStatus;

    async Task Because()
    {
        MapModel(RouteModels.Model(RouteModels.Command("PlaceOrder"), RouteModels.Query("OrderReport")));
        _before = Names();
        StageEndpointMapper.Map(_app, _surface);

        // These are the exact two ordinary mapping passes invoked by pinned UseCratisArc, with fresh mappers.
        _app.UseCommandEndpoints();
        _app.UseQueryEndpoints();
        _app.UseCommandEndpoints();
        _app.UseQueryEndpoints();
        _after = Names();
        BuildRouting();
        _commandStatus = (await Request("POST", "/api/orders/checkout/place-order/do-it")).Status;
        _queryStatus = (await Request("GET", "/api/orders/checkout/order-report/all-orders")).Status;
    }

    string[] Names() => [.. Endpoints().Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName ?? string.Empty).Order(StringComparer.Ordinal)];

    [Fact] void should_preserve_the_complete_endpoint_set() => _after.SequenceEqual(_before).ShouldBeTrue();
    [Fact] void should_have_no_duplicate_endpoint_names() => _after.Distinct().Count().ShouldEqual(_after.Length);
    [Fact] void should_have_no_duplicate_method_and_path_pairs() => Endpoints().SelectMany(endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Select(method => (method, endpoint.RoutePattern.RawText))).GroupBy(pair => pair).All(group => group.Count() == 1).ShouldBeTrue();
    [Fact] void should_still_select_one_command() => _commandStatus.ShouldEqual(200);
    [Fact] void should_still_select_one_query() => _queryStatus.ShouldEqual(403);
}
