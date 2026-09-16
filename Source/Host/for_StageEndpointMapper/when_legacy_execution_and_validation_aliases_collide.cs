// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_legacy_execution_and_validation_aliases_collide : given.a_routed_model
{
    int _ambiguousStatus;
    int _canonicalExecuteStatus;
    int _canonicalValidateStatus;

    async Task Because()
    {
        MapModel(RouteModels.WithFeatures(RouteModels.Feature(
            "Checkout",
            [RouteModels.Command("PlaceOrder")],
            RouteModels.Feature("Validate", [RouteModels.Command("CancelOrder")]))));
        _ambiguousStatus = (await Request("POST", "/api/orders/checkout/validate")).Status;
        _canonicalExecuteStatus = (await Request("POST", "/api/orders/checkout/validate/cancel-order/do-it")).Status;
        _canonicalValidateStatus = (await Request("POST", "/api/orders/checkout/place-order/do-it/validate")).Status;
    }

    [Fact] void should_omit_the_alias_owned_by_both_an_execution_and_a_validation_operation() => _ambiguousStatus.ShouldEqual(404);
    [Fact] void should_keep_the_canonical_execution() => _canonicalExecuteStatus.ShouldEqual(200);
    [Fact] void should_keep_the_canonical_validation() => _canonicalValidateStatus.ShouldEqual(200);
    [Fact] void should_invoke_only_the_two_canonical_operations() => _commands.Count.ShouldEqual(2);
    [Fact] void should_append_only_for_the_canonical_execution() => _appends.Count.ShouldEqual(1);
}
