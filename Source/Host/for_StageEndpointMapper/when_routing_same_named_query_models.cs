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
    readonly List<bool> _deniedWithoutData = [];
    readonly List<int> _legacyStatuses = [];

    async Task Because()
    {
        MapModel(RouteModels.Model(RouteModels.Query("CurrentOrders"), RouteModels.Query("ArchivedOrders")));
        foreach (var method in new[] { "GET", "QUERY" })
        {
            foreach (var slice in new[] { "current-orders", "archived-orders" })
            {
                foreach (var name in new[] { "get-order-by-id", "all-orders" })
                {
                    var response = await Request(method, $"/api/orders/checkout/{slice}/{name}?id=11111111-1111-1111-1111-111111111111", """{"arguments":{"id":"11111111-1111-1111-1111-111111111111"}}""");
                    _statuses.Add(response.Status);
                    using var document = JsonDocument.Parse(response.Body);
                    var root = document.RootElement;
                    _deniedWithoutData.Add(!root.GetProperty("isAuthorized").GetBoolean() && (!root.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null));
                }
            }
            _legacyStatuses.Add((await Request(method, "/api/orders/checkout/get-order-by-id")).Status);
            _legacyStatuses.Add((await Request(method, "/api/orders/checkout/all-orders")).Status);
        }
    }

    [Fact] void should_retain_both_read_model_types() => _queryProviders.Performers.Select(performer => performer.ReadModelType).Distinct().Count().ShouldEqual(2);
    [Fact] void should_retain_both_query_identities_for_each_read_model() => _queryProviders.Performers.Select(performer => performer.FullyQualifiedName).Distinct().Count().ShouldEqual(4);
    [Fact] void should_route_all_eight_canonical_method_and_path_combinations() => (_statuses.Count == 8 && _statuses.TrueForAll(status => status == 403)).ShouldBeTrue();
    [Fact] void should_preserve_authorization_denial_and_no_data() => _deniedWithoutData.TrueForAll(denied => denied).ShouldBeTrue();
    [Fact] void should_invoke_each_query_identity_over_both_transports() => (_queries.Count == 8 && _queries.GroupBy(context => context.Name).All(group => group.Count() == 2)).ShouldBeTrue();
    [Fact] void should_not_expose_ambiguous_legacy_queries() => (_legacyStatuses.Count == 4 && _legacyStatuses.TrueForAll(status => status == 404)).ShouldBeTrue();
    [Fact] void should_never_render_a_query_result() => _queryRenderers.DidNotReceive().Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<object>(), Arg.Any<IServiceProvider>());
}
