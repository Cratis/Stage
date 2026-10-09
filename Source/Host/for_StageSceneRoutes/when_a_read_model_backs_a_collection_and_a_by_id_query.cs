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

/// <summary>
/// A read model can back more than one modeled query: an argument-less collection and a "by id" lookup both
/// return the same read model. Arc names the conventional collection query from the read model's own plural
/// name rather than the author's chosen query name, so the collection's route can be the textually longer of
/// the two - here "all-work-item-summaries" is longer than "work-item-by-id" even though the collection, not
/// the lookup, is what the table element actually needs.
/// </summary>
public class when_a_read_model_backs_a_collection_and_a_by_id_query : Specification
{
    SceneApplication _scene = null!;

    void Establish()
    {
        var table = new ExternalComponent
        {
            Id = "list",
            Name = "Work items",
            ComponentName = "core:table",
            Properties = new Dictionary<string, object?>
            {
                [SceneElementProperties.TypeName] = "WorkItemSummary",
                ["isCollection"] = true,
                [SceneElementProperties.Query] = "AllWorkItems",
                ["by"] = null,
            },
            Slots = new Dictionary<string, IReadOnlyList<SceneElement>>()
        };
        var screen = new Screen(
            "WorkItemList",
            "AppShell",
            new Dictionary<string, IReadOnlyList<SceneElement>> { ["list"] = [table] },
            [],
            [],
            null);
        var endpoints = new StaticEndpointDataSource(
            Endpoint(
                "/api/workspaces/tracking/work-item-list/work-item-by-id",
                "GET",
                new NamedEndpoint("Workspaces.Tracking.WorkItemList.WorkItemSummary.WorkItemById")),
            Endpoint(
                "/api/workspaces/tracking/work-item-by-id",
                "GET",
                new NamedEndpoint("Workspaces.Tracking.WorkItemList.WorkItemSummary.WorkItemById.StageLegacy")),
            Endpoint(
                "/api/workspaces/tracking/work-item-list/all-work-item-summaries",
                "GET",
                new NamedEndpoint("Workspaces.Tracking.WorkItemList.WorkItemSummary.AllWorkItemSummaries")),
            Endpoint(
                "/api/workspaces/tracking/all-work-item-summaries",
                "GET",
                new NamedEndpoint("Workspaces.Tracking.WorkItemList.WorkItemSummary.AllWorkItemSummaries.StageLegacy")));

        _scene = StageSceneRoutes.WithRoutes(
            new SceneApplication([], [], [], [], [], [screen]),
            endpoints,
            NullLogger.Instance);
    }

    [Fact] void should_attach_the_collection_route() => TableProperties["route"].ShouldEqual("/api/workspaces/tracking/all-work-item-summaries");
    [Fact] void should_attach_the_get_method() => TableProperties["method"].ShouldEqual("GET");

    IReadOnlyDictionary<string, object?> TableProperties => Components.Single(_ => _.Id == "list").Properties;
    IEnumerable<ExternalComponent> Components => _scene.Screens.Single().SlotContent["list"].OfType<ExternalComponent>();

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
