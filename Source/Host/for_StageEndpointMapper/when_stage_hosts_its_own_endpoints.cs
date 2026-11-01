// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Microsoft.AspNetCore.Builder;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_stage_hosts_its_own_endpoints : given.a_routed_model
{
    string[] _paths = [];

    async Task Because()
    {
        MapModel(RouteModels.WithFeatures(RouteModels.Feature("Checkout", [RouteModels.Command("PlaceOrder")])));
        _app.MapGet("/stage/status", () => "ready");
        _app.MapGet("/stage/read-models/{id}", (string id) => id);
        _app.MapPost("/stage/semantic/specifications/run", () => "ok");
        _app.MapGet("/workbench", () => "workbench");
        _app.MapGet("/stagehands", () => "a modeled route that merely starts with the same letters");
        var response = await Request("GET", "/openapi/v1.json");
        using var document = JsonDocument.Parse(response.Body);
        _paths = [.. document.RootElement.GetProperty("paths").EnumerateObject().Select(path => path.Name)];
    }

    [Fact] void should_not_describe_the_stage_endpoints() => _paths.Where(path => path.StartsWith("/stage/", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_not_describe_the_workbench_proxy() => _paths.ShouldNotContain("/workbench");
    [Fact] void should_still_describe_the_models_own_operations() => _paths.ShouldContain("/api/orders/checkout/place-order/do-it");
    [Fact] void should_match_whole_segments_only() => _paths.ShouldContain("/stagehands");
}
