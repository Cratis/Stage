// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Api;
using Cratis.Stage.Contracts.Scene;
using Microsoft.AspNetCore.Routing;
using SceneElements = Cratis.Scene.Model.Elements;

namespace Cratis.Stage.Host;

/// <summary>
/// The scene served to a frontend, with the API route every element is backed by attached.
/// </summary>
/// <remarks>
/// <para>
/// The routes are read from the endpoints Arc actually registered rather than rebuilt from its naming
/// conventions. A convention re-implemented here would be a second answer to a question Arc already answers,
/// and the two would drift the moment either changed - a frontend calling a route nothing serves looks like a
/// broken application rather than a stale assumption.
/// </para>
/// <para>
/// Resolution happens on first read rather than while the application is being configured: the modeled
/// commands and queries become endpoints as the application starts, so a scene resolved during configuration
/// carries no routes at all.
/// </para>
/// </remarks>
/// <param name="scene">The scene to attach routes to.</param>
/// <param name="services">The application services, used to read the registered endpoints.</param>
/// <param name="logger">The logger used for the optional endpoint report.</param>
public sealed class StageSceneRoutes(SceneApplication scene, IServiceProvider services, ILogger logger)
{
    /// <summary>
    /// The environment variable that makes the resolver report every endpoint it considered.
    /// </summary>
    public const string DiagnosticsVariable = "STAGE_ROUTE_DIAGNOSTICS";

    /// <summary>
    /// The property an element carries when its route could not be resolved: <c language="csharp">unresolved</c> or
    /// <c language="csharp">ambiguous</c>.
    /// </summary>
    public const string RouteStatusProperty = "routeStatus";

    /// <summary>
    /// The property an element carries explaining why its route could not be resolved.
    /// </summary>
    public const string RouteDiagnosticProperty = "routeDiagnostic";

    readonly Lazy<NamedRoutes> _commandRoutes = new(
        () => Named(services.GetRequiredService<EndpointDataSource>(), "POST"),
        LazyThreadSafetyMode.ExecutionAndPublication);

    readonly Lazy<NamedRoutes> _queryRoutes = new(
        () => Named(services.GetRequiredService<EndpointDataSource>(), "GET"),
        LazyThreadSafetyMode.ExecutionAndPublication);

    readonly Lazy<SceneApplication> _resolved = new(
        () => WithRoutes(scene, services.GetRequiredService<EndpointDataSource>(), logger),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Gets the scene with routes attached, resolving them the first time it is read.
    /// </summary>
    public SceneApplication Scene => _resolved.Value;

    /// <summary>
    /// Gets the route each modeled command is posted to, by command name.
    /// </summary>
    /// <remarks>
    /// An element carries the route it is backed by, which is enough for an element that <em>is</em> a command.
    /// An interaction is not: it names a command that may live anywhere in the model, attached to a button that
    /// knows nothing about it. Answering "where does CancelInvoice go" needs a lookup by name, and this is it.
    /// <para>
    /// Resolved from the same endpoint set the elements use, so an interaction and an action cannot disagree
    /// about where the same command lives.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, string> CommandRoutes => _commandRoutes.Value.Routes;

    /// <summary>
    /// Gets the route each modeled query is read from, by query name.
    /// </summary>
    public IReadOnlyDictionary<string, string> QueryRoutes => _queryRoutes.Value.Routes;

    /// <summary>
    /// Gets why a name is missing from <see cref="CommandRoutes"/> or <see cref="QueryRoutes"/>: every name that
    /// several distinct operations answer to, which is left out rather than resolved to one of them.
    /// </summary>
    public IReadOnlyList<string> Diagnostics => [.. _commandRoutes.Value.Diagnostics, .. _queryRoutes.Value.Diagnostics];

    /// <summary>
    /// Builds the name to route lookup for one verb.
    /// </summary>
    /// <param name="endpoints">The endpoints the application registered.</param>
    /// <param name="method">The verb to collect.</param>
    /// <returns>The route by name.</returns>
    /// <remarks>
    /// A name is the operation's own - the query or command name - so <c language="csharp">AllWorkItems</c> and
    /// <c language="csharp">WorkItemById</c> are two entries even though one read model backs both. A name that
    /// several distinct operations answer to (the same command name in two slices, say) is left out: choosing one
    /// would send the caller to whichever happens to have the shorter route.
    /// </remarks>
    public static IReadOnlyDictionary<string, string> RoutesByName(EndpointDataSource endpoints, string method) =>
        Named(endpoints, method).Routes;

    /// <summary>
    /// Attaches the route of every element that names a modeled artifact.
    /// </summary>
    /// <param name="scene">The scene to attach routes to.</param>
    /// <param name="endpoints">The registered endpoints.</param>
    /// <param name="logger">The logger used for the endpoint report and route diagnostics.</param>
    /// <returns>The scene, with routes attached where exactly one was found and a diagnostic where none or several were.</returns>
    public static SceneApplication WithRoutes(SceneApplication scene, EndpointDataSource endpoints, ILogger logger)
    {
        var candidates = StageRouteCandidates.From(endpoints);

        if (string.Equals(Environment.GetEnvironmentVariable(DiagnosticsVariable), "1", StringComparison.Ordinal))
        {
            foreach (var candidate in candidates)
            {
                StageLog.EndpointConsidered(logger, candidate.Method, candidate.Route, string.Join(" | ", candidate.Names));
            }
        }

        if (candidates.Count == 0)
        {
            return scene;
        }

        return scene with
        {
            Screens =
            [
                .. scene.Screens.Select(screen => screen with
                {
                    SlotContent = screen.SlotContent.ToDictionary(
                        slot => slot.Key,
                        slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(element => WithRoute(element, candidates, logger))],
                        StringComparer.Ordinal)
                })
            ]
        };
    }

    static NamedRoutes Named(EndpointDataSource endpoints, string method)
    {
        var operations = StageRouteCandidates.From(endpoints)
            .Where(candidate => candidate.Identity is not null && string.Equals(candidate.Method, method, StringComparison.Ordinal))
            .GroupBy(candidate => candidate.Identity!, StringComparer.Ordinal)
            .Select(operation => (Identity: operation.Key, operation.OrderBy(candidate => candidate.Route.Length).First().Route));

        var routes = new Dictionary<string, string>(StringComparer.Ordinal);
        var diagnostics = new List<string>();
        foreach (var name in operations.GroupBy(operation => StageRouteCandidates.SimpleName(operation.Identity), StringComparer.Ordinal))
        {
            var answering = name.ToArray();
            if (answering.Length == 1)
            {
                routes[name.Key] = answering[0].Route;
                continue;
            }

            diagnostics.Add($"{method} '{name.Key}' is ambiguous between {string.Join(", ", answering.Select(operation => operation.Identity).Order(StringComparer.Ordinal))}; it is not resolved by name.");
        }

        return new(routes, diagnostics);
    }

    static SceneElements.SceneElement WithRoute(SceneElements.SceneElement element, IReadOnlyList<StageRouteCandidate> candidates, ILogger logger)
    {
        if (element is not SceneElements.ExternalComponent component)
        {
            return element;
        }

        var nested = component.Slots.ToDictionary(
            slot => slot.Key,
            slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(child => WithRoute(child, candidates, logger))],
            StringComparer.Ordinal);

        if (Request(component) is not { } request)
        {
            return component with { Slots = nested };
        }

        var properties = new Dictionary<string, object?>(component.Properties, StringComparer.Ordinal);
        var resolution = StageRouteCandidates.Resolve(candidates, request.Method, request.Selects);
        if (resolution is { Match: null, IsAmbiguous: false } && request.Equivalent is { } equivalent)
        {
            resolution = StageRouteCandidates.Resolve(candidates, request.Method, equivalent);
        }

        if (resolution.Match is { } match)
        {
            properties[SceneSynthesizer.RouteProperty] = match.Route;
            properties["method"] = match.Method;
            properties.Remove(RouteStatusProperty);
            properties.Remove(RouteDiagnosticProperty);
        }
        else
        {
            // No route is attached rather than a guessed one: a list bound to the wrong query on the same read
            // model renders, calls a route that exists, and silently shows the wrong data.
            var diagnostic = resolution.IsAmbiguous
                ? $"{request.Description} is ambiguous between {string.Join(", ", resolution.Candidates)}."
                : $"{request.Description} does not match any registered {request.Method} endpoint.";
            properties[RouteStatusProperty] = resolution.IsAmbiguous ? "ambiguous" : "unresolved";
            properties[RouteDiagnosticProperty] = diagnostic;
            StageLog.RouteNotResolved(logger, component.Id, diagnostic);
        }

        return component with { Properties = properties, Slots = nested };
    }

    static RouteRequest? Request(SceneElements.ExternalComponent component)
    {
        var typeName = Text(component, SceneSynthesizer.TypeNameProperty);
        if (IsCommandComponent(component.ComponentName))
        {
            // A synthesized action carries the command's full type name; an authored one only its name.
            if (typeName is not null)
            {
                return new("POST", identity => StageRouteCandidates.Names(identity, typeName), $"Command type '{typeName}'");
            }

            return Text(component, "command") is { } command
                ? new("POST", identity => StageRouteCandidates.SimpleName(identity) == command, $"Command '{command}'")
                : null;
        }

        // An authored data binding names its query: that query, and only that query, is what it reads. The read
        // model it returns narrows the search when the element carries one, so a same-named query over another
        // read model is not taken for it.
        if (Text(component, SceneElementProperties.Query) is { } query)
        {
            return new(
                "GET",
                identity => StageRouteCandidates.SimpleName(identity) == query && (typeName is null || StageRouteCandidates.IsOwnedBy(identity, typeName)),
                $"Query '{query}'{(typeName is null ? string.Empty : $" over '{typeName}'")}")
            {
                Equivalent = UnkeyedCollection(component, typeName)
            };
        }

        // A synthesized element names only the read model; what it shows is that read model's collection.
        if (typeName is not null)
        {
            var collection = StageRouteCandidates.ConventionalCollection(typeName);
            return new(
                "GET",
                identity => StageRouteCandidates.SimpleName(identity) == collection && StageRouteCandidates.IsOwnedBy(identity, typeName),
                $"Collection query '{collection}' over '{typeName}'");
        }

        return null;
    }

    // An unkeyed collection query over a read model answers every instance of it - exactly what the read model's
    // conventional All<ReadModels> answers. When the host serves no endpoint under the query's own name, that
    // conventional collection is the same rows by definition, not a guess: it is one exact identity, never a
    // single-item lookup. A keyed binding (one with a by-parameter) or a single-result one has no such equivalent,
    // so it stays unresolved rather than being widened to the unfiltered set.
    static Func<string, bool>? UnkeyedCollection(SceneElements.ExternalComponent component, string? typeName)
    {
        if (typeName is null ||
            Text(component, "by") is not null ||
            !component.Properties.TryGetValue("isCollection", out var isCollection) ||
            isCollection is not true)
        {
            return null;
        }

        var collection = StageRouteCandidates.ConventionalCollection(typeName);

        return identity => StageRouteCandidates.SimpleName(identity) == collection && StageRouteCandidates.IsOwnedBy(identity, typeName);
    }

    static string? Text(SceneElements.ExternalComponent component, string property) =>
        component.Properties.TryGetValue(property, out var value) && value is string { Length: > 0 } text ? text : null;

    static bool IsCommandComponent(string componentName) =>
        string.Equals(componentName, "core:action", StringComparison.Ordinal) ||
        string.Equals(componentName, "Stage:commandForm", StringComparison.Ordinal);

    sealed record RouteRequest(string Method, Func<string, bool> Selects, string Description)
    {
        public Func<string, bool>? Equivalent { get; init; }
    }

    sealed record NamedRoutes(IReadOnlyDictionary<string, string> Routes, IReadOnlyList<string> Diagnostics);
}
