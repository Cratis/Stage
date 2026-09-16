// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_routing_same_named_source_commands : given.a_routed_model
{
    EventModel _model = null!;
    readonly List<int> _statuses = [];

    void Establish() => _model = EventModelLoader.LoadFromSource(RouteModels.SameNamedCommands);

    async Task Because()
    {
        MapModel(_model);
        foreach (var slice in new[] { "place-order", "cancel-order" })
        {
            _statuses.Add((await Request("POST", $"/api/orders/checkout/{slice}/do-it", """{"orderId":"11111111-1111-1111-1111-111111111111"}""")).Status);
        }
        foreach (var slice in new[] { "place-order", "cancel-order" })
        {
            _statuses.Add((await Request("POST", $"/api/orders/checkout/{slice}/do-it/validate")).Status);
        }
        _statuses.Add((await Request("POST", "/api/orders/checkout/do-it")).Status);
        _statuses.Add((await Request("POST", "/api/orders/checkout/do-it/validate")).Status);
    }

    [Fact] void should_preserve_both_source_commands() => StageModelWalker.Slices(_model).Select(slice => slice.Slice.Command!.Name).ToArray().ShouldEqual(["DoIt", "DoIt"]);
    [Fact] void should_select_both_canonical_commands_and_validation_routes() => _statuses.Take(4).ToArray().ShouldEqual([200, 200, 200, 200]);
    [Fact] void should_leave_both_ambiguous_legacy_routes_unmapped() => _statuses.Skip(4).ToArray().ShouldEqual([404, 404]);
    [Fact] void should_never_invoke_any_command_for_the_ambiguous_aliases() => _commands.Count.ShouldEqual(4);
    [Fact] void should_keep_distinct_emitted_types() => _commandProviders.Handlers.Select(handler => handler.CommandType).Distinct().Count().ShouldEqual(2);
    [Fact] void should_bind_place_order() => _commands[0].Type.FullName.ShouldEqual("Stage.Orders.Checkout.PlaceOrder.DoIt");
    [Fact] void should_bind_cancel_order() => _commands[1].Type.FullName.ShouldEqual("Stage.Orders.Checkout.CancelOrder.DoIt");
    [Fact] void should_validate_place_order_without_dispatching_cancel_order() => _commands[2].Type.ShouldEqual(_commands[0].Type);
    [Fact] void should_validate_cancel_order_without_dispatching_place_order() => _commands[3].Type.ShouldEqual(_commands[1].Type);
    [Fact] void should_append_only_for_execution() => _appends.Count.ShouldEqual(2);
    [Fact] void should_attribute_the_first_append_to_the_bound_place_order_type() => _appends[0].BoundType.ShouldEqual(_commands[0].Type);
    [Fact] void should_attribute_the_second_append_to_the_bound_cancel_order_type() => _appends[1].BoundType.ShouldEqual(_commands[1].Type);
    [Fact] void should_preserve_equal_event_names_without_using_them_as_dispatch_identity() => _appends.Select(append => append.Events.Single().EventType).ToArray().ShouldEqual(["ItHappened", "ItHappened"]);
}
