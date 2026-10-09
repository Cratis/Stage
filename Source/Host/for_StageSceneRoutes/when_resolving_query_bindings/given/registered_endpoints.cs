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
using Microsoft.Extensions.Primitives;

namespace Cratis.Stage.Host.for_StageSceneRoutes.when_resolving_query_bindings.given;

/// <summary>
/// Endpoints named the way Arc names them, including a legacy alias per operation, for scene elements to be
/// resolved against.
/// </summary>
public class registered_endpoints : Specification
{
    protected readonly List<RouteEndpoint> _endpoints = [];
    protected SceneApplication _scene = null!;

    protected void Query(string identity, string canonical, string legacy)
    {
        _endpoints.Add(Endpoint(canonical, "GET", identity));
        _endpoints.Add(Endpoint(legacy, "GET", $"{identity}.StageLegacy"));
    }

    protected void Resolve(params ExternalComponent[] elements)
    {
        var screen = new Screen(
            "Screen",
            "AppShell",
            new Dictionary<string, IReadOnlyList<SceneElement>> { ["content"] = elements },
            [],
            []);
        _scene = StageSceneRoutes.WithRoutes(new SceneApplication([], [], [], [], [], [screen]), Endpoints(), NullLogger.Instance);
    }

    protected EndpointDataSource Endpoints() => new StaticEndpointDataSource([.. _endpoints]);

    protected IReadOnlyDictionary<string, object?> Properties(string id) =>
        _scene.Screens.Single().SlotContent["content"].OfType<ExternalComponent>().Single(component => component.Id == id).Properties;

    protected static ExternalComponent Data(string id, string query, string? typeName = null) => new()
    {
        Id = id,
        Name = id,
        ComponentName = "core:data",
        Properties = typeName is null
            ? new Dictionary<string, object?> { ["query"] = query }
            : new Dictionary<string, object?> { ["query"] = query, [SceneElementProperties.TypeName] = typeName },
        Slots = new Dictionary<string, IReadOnlyList<SceneElement>>()
    };

    protected static ExternalComponent Table(string id, string typeName) => new()
    {
        Id = id,
        Name = id,
        ComponentName = "core:table",
        Properties = new Dictionary<string, object?> { [SceneElementProperties.TypeName] = typeName },
        Slots = new Dictionary<string, IReadOnlyList<SceneElement>>()
    };

    static RouteEndpoint Endpoint(string pattern, string method, string name) => new(
        _ => Task.CompletedTask,
        RoutePatternFactory.Parse(pattern),
        0,
        new EndpointMetadataCollection(new EndpointNameMetadata(name), new HttpMethodMetadata([method])),
        pattern);

    sealed class StaticEndpointDataSource(IReadOnlyList<Endpoint> endpoints) : EndpointDataSource
    {
        public override IReadOnlyList<Endpoint> Endpoints { get; } = endpoints;

        public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);
    }
}
