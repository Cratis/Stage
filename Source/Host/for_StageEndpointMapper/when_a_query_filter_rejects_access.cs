// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_a_query_filter_rejects_access : given.a_routed_model
{
    readonly List<int> _statuses = [];
    readonly List<bool> _deniedWithoutData = [];

    async Task Because()
    {
        _rejectQueries = true;
        MapModel(RouteModels.Model(RouteModels.Query("OrderReport")));
        foreach (var method in new[] { "GET", "QUERY" })
        {
            foreach (var path in new[] { "/api/orders/checkout/order-report/all-orders", "/api/orders/checkout/all-orders" })
            {
                var response = await Request(method, path);
                _statuses.Add(response.Status);
                using var document = JsonDocument.Parse(response.Body);
                var result = document.RootElement;
                _deniedWithoutData.Add(!result.GetProperty("isAuthorized").GetBoolean() &&
                    (!result.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null));
            }
        }
    }

    [Fact] void should_deny_canonical_and_legacy_routes_over_both_transports() => _statuses.SequenceEqual([403, 403, 403, 403]).ShouldBeTrue();
    [Fact] void should_expose_no_data() => (_deniedWithoutData.Count == 4 && _deniedWithoutData.TrueForAll(denied => denied)).ShouldBeTrue();
    [Fact] void should_never_read_chronicle() => _readModelRequests.ShouldBeEmpty();
    [Fact] void should_never_render_a_query_result() => _queryRenderers.DidNotReceive().Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<object>(), Arg.Any<IServiceProvider>());
}
