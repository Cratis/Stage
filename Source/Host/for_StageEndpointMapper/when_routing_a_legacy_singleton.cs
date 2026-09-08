// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_routing_a_legacy_singleton : given.a_routed_model
{
    readonly List<int> _statuses = [];

    async Task Because()
    {
        MapModel(RouteModels.Model(RouteModels.Command("PlaceOrder")));
        foreach (var path in new[] { "/api/orders/checkout", "/api/orders/checkout/validate", "/api/orders/checkout/place-order/do-it", "/api/orders/checkout/do-it" })
        {
            _statuses.Add((await Request("POST", path)).Status);
        }
    }

    [Fact] void should_preserve_the_actual_feature_only_legacy_route() => _statuses[0].ShouldEqual(200);
    [Fact] void should_preserve_feature_only_validation() => _statuses[1].ShouldEqual(200);
    [Fact] void should_always_include_the_command_name_in_the_canonical_route() => _statuses[2].ShouldEqual(200);
    [Fact] void should_not_invent_a_documented_but_never_mapped_singleton_alias() => _statuses[3].ShouldEqual(404);
    [Fact] void should_reuse_the_canonical_command_type_for_both_aliases() => _commands.Select(context => context.Type).Distinct().Count().ShouldEqual(1);
    [Fact] void should_not_append_during_validation_or_the_unmapped_request() => _appends.Count.ShouldEqual(2);
}
