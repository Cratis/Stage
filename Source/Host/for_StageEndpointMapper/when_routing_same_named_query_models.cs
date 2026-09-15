// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_routing_same_named_query_models : given.a_routed_model
{
    readonly List<int> _statuses = [];
    readonly List<bool> _correctlyScoped = [];
    readonly List<int> _legacyStatuses = [];

    async Task Because()
    {
        var current = RouteModels.Query("CurrentOrders");
        var archived = RouteModels.Query("ArchivedOrders");
        MapModel(RouteModels.Model(current, archived));
        foreach (var method in new[] { "GET", "QUERY" })
        {
            foreach (var slice in new[] { "current-orders", "archived-orders" })
            {
                foreach (var name in new[] { "get-order-by-id", "all-orders" })
                {
                    var response = await Request(method, $"/api/orders/checkout/{slice}/{name}?id=11111111-1111-1111-1111-111111111111", """{"arguments":{"id":"11111111-1111-1111-1111-111111111111"}}""");
                    _statuses.Add(response.Status);
                    Assert.True(response.Status == 200, $"{method} {slice}/{name}: HTTP {response.Status}: {response.Body}");
                    using var document = JsonDocument.Parse(response.Body);
                    var root = document.RootElement;
                    Assert.True(root.TryGetProperty("data", out var data), $"Successful query omitted data: {response.Body}");
                    var instance = data.ValueKind == JsonValueKind.Array ? data.EnumerateArray().Single() : data;
                    var expectedOwner = (slice == "current-orders" ? current : archived).ReadModel!.Id.ToString();
                    _correctlyScoped.Add(root.GetProperty("isAuthorized").GetBoolean() && instance.GetProperty("owner").GetString() == expectedOwner);
                }
            }
            _legacyStatuses.Add((await Request(method, "/api/orders/checkout/get-order-by-id")).Status);
            _legacyStatuses.Add((await Request(method, "/api/orders/checkout/all-orders")).Status);
        }
    }

    [Fact] void should_retain_both_read_model_types() => _queryProviders.Performers.Select(performer => performer.ReadModelType).Distinct().Count().ShouldEqual(2);
    [Fact] void should_retain_both_query_identities_for_each_read_model() => _queryProviders.Performers.Select(performer => performer.FullyQualifiedName).Distinct().Count().ShouldEqual(4);
    [Fact] void should_route_all_eight_canonical_method_and_path_combinations() => (_statuses.Count == 8 && _statuses.TrueForAll(status => status == 200)).ShouldBeTrue();
    [Fact] void should_return_the_correct_persistent_read_model_for_each_slice() => (_correctlyScoped.Count == 8 && _correctlyScoped.TrueForAll(correct => correct)).ShouldBeTrue();
    [Fact] void should_read_each_persistent_model_over_both_transports_and_query_shapes() => (_readModelRequests.Count == 8 && _readModelRequests.GroupBy(id => id).Count() == 2 && _readModelRequests.GroupBy(id => id).All(group => group.Count() == 4)).ShouldBeTrue();
    [Fact] void should_invoke_each_query_identity_over_both_transports() => (_queries.Count == 8 && _queries.GroupBy(context => context.Name).All(group => group.Count() == 2)).ShouldBeTrue();
    [Fact] void should_not_expose_ambiguous_legacy_queries() => (_legacyStatuses.Count == 4 && _legacyStatuses.TrueForAll(status => status == 404)).ShouldBeTrue();
    [Fact] void should_render_each_successful_query_result() => _queryRenderers.Received(8).Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<object>(), Arg.Any<IServiceProvider>());
}
