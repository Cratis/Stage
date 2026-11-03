// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Xunit;

namespace Cratis.Stage.Host.for_SafeSceneApplication;

public class when_requesting_the_guarded_action_command : for_StageEndpointMapper.given.a_routed_model
{
    int _status;

    void Establish() => MapModel(RouteModels.Model(RouteModels.Command("Close", "CloseWorkItem")));

    async Task Because()
    {
        var response = await Request("POST", "/api/orders/checkout/close/close-work-item", """{"orderId":"11111111-1111-1111-1111-111111111111"}""");
        _status = response.Status;
    }

    [Fact] void should_accept_the_command() => _status.ShouldEqual(200);
    [Fact] void should_execute_the_requested_command() => _commands.Count.ShouldEqual(1);
    [Fact] void should_append_the_produced_events() => _appends.Count.ShouldEqual(1);
}
