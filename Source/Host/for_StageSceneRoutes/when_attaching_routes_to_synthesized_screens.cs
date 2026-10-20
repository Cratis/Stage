// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cratis.Stage.Host.for_StageSceneRoutes;

public class when_attaching_routes_to_synthesized_screens : for_StageEndpointMapper.given.a_routed_model
{
    SceneApplication _synthesized = null!;
    SceneApplication _resolved = null!;
    EndpointDataSource _endpoints = null!;

    void Establish()
    {
        var command = RouteModels.Command("PlaceOrder", "PlaceOrder");
        command = command with { Command = command.Command! with { Schema = "{}" } };
        var model = RouteModels.Model(command, RouteModels.Query("OrderReport"));
        MapModel(model);
        _endpoints = new CompositeEndpointDataSource(((IEndpointRouteBuilder)_app).DataSources);
        _synthesized = SceneSynthesizer.Synthesize(new SceneApplication([], [], [], [], [], []), model);
    }

    void Because() => _resolved = StageSceneRoutes.WithRoutes(_synthesized, _endpoints, NullLogger.Instance);

    [Fact] void should_carry_the_emitted_read_model_type_name() => Component(_synthesized, "core:table").Properties[SceneSynthesizer.TypeNameProperty].ShouldEqual("Stage.Orders.Checkout.OrderReport.Order");
    [Fact] void should_carry_the_emitted_command_type_name() => Component(_synthesized, "core:action").Properties[SceneSynthesizer.TypeNameProperty].ShouldEqual("Stage.Orders.Checkout.PlaceOrder.PlaceOrder");
    [Fact] void should_attach_the_collection_route_to_the_table() => Properties("core:table").GetValueOrDefault("route").ShouldEqual("/api/orders/checkout/all-orders");
    [Fact] void should_attach_the_get_method_to_the_table() => Properties("core:table").GetValueOrDefault("method").ShouldEqual("GET");
    [Fact] void should_attach_the_collection_route_to_the_data_binding() => Properties("core:data").GetValueOrDefault("route").ShouldEqual("/api/orders/checkout/all-orders");
    [Fact] void should_attach_the_execution_route_to_the_action() => Properties("core:action").GetValueOrDefault("route").ShouldEqual("/api/orders/checkout");
    [Fact] void should_attach_the_post_method_to_the_action() => Properties("core:action").GetValueOrDefault("method").ShouldEqual("POST");
    [Fact] void should_leave_the_table_without_a_route_failure() => Properties("core:table").ContainsKey(StageSceneRoutes.RouteStatusProperty).ShouldBeFalse();
    [Fact] void should_leave_the_action_without_a_route_failure() => Properties("core:action").ContainsKey(StageSceneRoutes.RouteStatusProperty).ShouldBeFalse();
    [Fact] void should_keep_the_qualified_execution_identity() => StageRouteCandidates.From(_endpoints).Select(candidate => candidate.Identity).ShouldContain("ExecuteStage.Orders.Checkout.PlaceOrder.PlaceOrder");

    IReadOnlyDictionary<string, object?> Properties(string componentName) => Component(_resolved, componentName).Properties;

    static ExternalComponent Component(SceneApplication scene, string componentName) => scene.Screens
        .SelectMany(screen => screen.SlotContent.Values.SelectMany(elements => elements))
        .OfType<ExternalComponent>()
        .Single(component => component.ComponentName == componentName);
}
