// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cratis.Stage.Host.for_StageSceneRoutes;

public class when_attaching_routes_to_authored_actions : Specification
{
    SceneApplication _scene = null!;
    IReadOnlyDictionary<string, string> _commandRoutes = null!;

    void Establish()
    {
        var action = new ExternalComponent
        {
            Id = "create",
            Name = "Create work item",
            ComponentName = "core:action",
            Properties = new Dictionary<string, object?> { ["command"] = "CreateWorkItem" },
            Slots = new Dictionary<string, IReadOnlyList<SceneElement>>()
        };
        var form = new ExternalComponent
        {
            Id = "create-form",
            Name = "Create work item form",
            ComponentName = "Stage:commandForm",
            Properties = new Dictionary<string, object?> { ["command"] = "CreateWorkItem" },
            Slots = new Dictionary<string, IReadOnlyList<SceneElement>>()
        };
        var screen = new Screen(
            "WorkItemList",
            "AppShell",
            new Dictionary<string, IReadOnlyList<SceneElement>> { ["content"] = [action, form] },
            [],
            [],
            null);
        var endpoints = new StaticEndpointDataSource(
            Endpoint("/api/workspaces/tracking/create-work-item", "POST", new NamedEndpoint("Workspaces.Tracking.CreateWorkItem.CreateWorkItem.StageLegacy")),
            Endpoint("/api/workspaces/tracking/create-work-item/create-work-item", "POST", new NamedEndpoint("Workspaces.Tracking.CreateWorkItem.CreateWorkItem")),
            Endpoint("/api/workspaces/tracking/work-item-list/all-work-items", "GET", "AllWorkItems"));

        _commandRoutes = StageSceneRoutes.RoutesByName(endpoints, "POST");
        _scene = StageSceneRoutes.WithRoutes(
            new SceneApplication([], [], [], [], [], [screen]),
            endpoints,
            NullLogger.Instance);
    }

    [Fact] void should_attach_the_command_route() => ActionProperties["route"].ShouldEqual("/api/workspaces/tracking/create-work-item");
    [Fact] void should_attach_the_post_method() => ActionProperties["method"].ShouldEqual("POST");
    [Fact] void should_attach_the_command_form_route() => FormProperties["route"].ShouldEqual("/api/workspaces/tracking/create-work-item");
    [Fact] void should_attach_the_command_form_post_method() => FormProperties["method"].ShouldEqual("POST");
    [Fact] void should_expose_the_same_command_route_by_command_name() => _commandRoutes["CreateWorkItem"].ShouldEqual("/api/workspaces/tracking/create-work-item");

    IReadOnlyDictionary<string, object?> ActionProperties => Components.Single(_ => _.Id == "create").Properties;
    IReadOnlyDictionary<string, object?> FormProperties => Components.Single(_ => _.Id == "create-form").Properties;
    IEnumerable<ExternalComponent> Components => _scene.Screens.Single().SlotContent["content"].OfType<ExternalComponent>();

    static RouteEndpoint Endpoint(string pattern, string method, params object[] metadata) => new(
        _ => Task.CompletedTask,
        RoutePatternFactory.Parse(pattern),
        0,
        new EndpointMetadataCollection([.. metadata, new HttpMethodMetadata([method])]),
        pattern);

    sealed class StaticEndpointDataSource(params Endpoint[] endpoints) : EndpointDataSource
    {
        public override IReadOnlyList<Endpoint> Endpoints { get; } = endpoints;
        public override Microsoft.Extensions.Primitives.IChangeToken GetChangeToken() => new Microsoft.Extensions.Primitives.CancellationChangeToken(CancellationToken.None);
    }

    sealed record NamedEndpoint(string EndpointName) : IEndpointNameMetadata;
}
