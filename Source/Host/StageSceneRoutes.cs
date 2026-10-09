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

    readonly Lazy<IReadOnlyDictionary<string, string>> _commandRoutes = new(
        () => RoutesByName(services.GetRequiredService<EndpointDataSource>(), "POST"),
        LazyThreadSafetyMode.ExecutionAndPublication);

    readonly Lazy<IReadOnlyDictionary<string, string>> _queryRoutes = new(
        () => RoutesByName(services.GetRequiredService<EndpointDataSource>(), "GET"),
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
    public IReadOnlyDictionary<string, string> CommandRoutes => _commandRoutes.Value;

    /// <summary>
    /// Gets the route each modeled query is read from, by read model name.
    /// </summary>
    public IReadOnlyDictionary<string, string> QueryRoutes => _queryRoutes.Value;

    /// <summary>
    /// Builds the name to route lookup for one verb.
    /// </summary>
    /// <param name="endpoints">The endpoints the application registered.</param>
    /// <param name="method">The verb to collect.</param>
    /// <returns>The route by name.</returns>
    /// <remarks>
    /// The shortest route wins a tie for the same reason it does when resolving an element's: a read model
    /// backs both a collection route and a by-id route, and the collection is the one without an argument
    /// nobody supplied.
    /// </remarks>
    public static IReadOnlyDictionary<string, string> RoutesByName(EndpointDataSource endpoints, string method)
    {
        var routes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var candidate in Candidates(endpoints)
            .Where(candidate => string.Equals(candidate.Method, method, StringComparison.Ordinal))
            .OrderBy(candidate => candidate.Route.Length))
        {
            foreach (var name in candidate.Names)
            {
                foreach (var alias in AliasesOf(name))
                {
                    routes.TryAdd(alias, candidate.Route);
                }
            }
        }

        return routes;
    }

    /// <summary>
    /// Attaches the route of every element that names a modeled artifact.
    /// </summary>
    /// <param name="scene">The scene to attach routes to.</param>
    /// <param name="endpoints">The registered endpoints.</param>
    /// <param name="logger">The logger used for the optional endpoint report.</param>
    /// <returns>The scene, with routes attached where one was found.</returns>
    public static SceneApplication WithRoutes(SceneApplication scene, EndpointDataSource endpoints, ILogger logger)
    {
        var candidates = Candidates(endpoints);

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
                        slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(element => WithRoute(element, candidates))],
                        StringComparer.Ordinal)
                })
            ]
        };
    }

    static IEnumerable<string> AliasesOf(string name)
    {
        var simple = name[(LastSeparator(name) + 1)..];
        if (simple.Length > 0)
        {
            yield return simple;
        }

        if (string.Equals(simple, "StageLegacy", StringComparison.Ordinal))
        {
            var separator = LastSeparator(name);
            var withoutSuffix = separator > 0 ? name[..separator] : string.Empty;
            var legacyCommand = withoutSuffix[(LastSeparator(withoutSuffix) + 1)..];
            if (legacyCommand.Length > 0)
            {
                yield return legacyCommand;
            }
        }
    }

    static int LastSeparator(string name) => Math.Max(name.LastIndexOf('.'), name.LastIndexOf('+'));

    static SceneElements.SceneElement WithRoute(SceneElements.SceneElement element, IReadOnlyList<EndpointCandidate> candidates)
    {
        if (element is not SceneElements.ExternalComponent component)
        {
            return element;
        }

        var nested = component.Slots.ToDictionary(
            slot => slot.Key,
            slot => (IReadOnlyList<SceneElements.SceneElement>)[.. slot.Value.Select(child => WithRoute(child, candidates))],
            StringComparer.Ordinal);

        if (RouteMatch(component, candidates) is { } match)
        {
            var properties = new Dictionary<string, object?>(component.Properties, StringComparer.Ordinal)
            {
                [SceneSynthesizer.RouteProperty] = match.Route,
                ["method"] = match.Method
            };

            return component with { Properties = properties, Slots = nested };
        }

        return component with { Slots = nested };
    }

    static EndpointCandidate? RouteMatch(SceneElements.ExternalComponent component, IReadOnlyList<EndpointCandidate> candidates)
    {
        // A read model can back more than one modeled query - an observable collection and a by-id lookup both
        // returning the same read model, for example - so the query's own name, when the element carries one,
        // disambiguates where the read model's type name alone cannot. Tried first and used only if it actually
        // resolves, so an element whose query name happens not to match any candidate still falls back below
        // rather than silently losing its route.
        if (component.Properties.TryGetValue(SceneElementProperties.Query, out var queryName) &&
            queryName is string query &&
            Resolve(candidates, query, component.ComponentName) is { } byQuery)
        {
            return byQuery;
        }

        if (component.Properties.TryGetValue(SceneSynthesizer.TypeNameProperty, out var typeName) && typeName is string namedType)
        {
            return Resolve(candidates, namedType, component.ComponentName);
        }

        return IsCommandComponent(component) && component.Properties.TryGetValue("command", out var command) && command is string commandName
            ? Resolve(candidates, commandName, component.ComponentName)
            : null;
    }

    static bool IsCommandComponent(SceneElements.ExternalComponent component) => IsCommandComponent(component.ComponentName);

    static bool IsCommandComponent(string componentName) =>
        string.Equals(componentName, "core:action", StringComparison.Ordinal) ||
        string.Equals(componentName, "Stage:commandForm", StringComparison.Ordinal);

    static EndpointCandidate? Resolve(IReadOnlyList<EndpointCandidate> candidates, string typeName, string componentName)
    {
        // A command is posted; a read model is read. Matching the verb as well keeps a command's execute route
        // from answering for a table, and a query route from being posted to.
        var method = IsCommandComponent(componentName) ? "POST" : "GET";
        var matching = candidates
            .Where(candidate =>
                string.Equals(candidate.Method, method, StringComparison.Ordinal) &&
                candidate.Names.Any(name => name.Contains(typeName, StringComparison.Ordinal)))
            .ToList();

        // A read model backs both an "all" route and a "by id" route, and both carry the read model's type name
        // somewhere in their full identity because they share the same slice-qualified namespace prefix.
        // Preferring the candidates that are not themselves a single-item lookup picks the collection query
        // even when an "all" route's text is the longer of the two - pluralizing a longer read model name can
        // make it longer than a short "by id" route, so route length alone cannot tell them apart.
        var withoutById = matching.Where(candidate => !IsByIdLookup(candidate)).ToList();
        if (withoutById.Count > 0)
        {
            matching = withoutById;
        }

        // Among what is left - the collection route's own canonical and legacy aliases, typically - the
        // canonical one is the shorter, since no alias here takes an argument nobody supplied.
        matching.Sort((left, right) => left.Route.Length.CompareTo(right.Route.Length));

        return matching.Count > 0 ? matching[0] : null;
    }

    // Every single-item lookup Arc generates for a read model - whether explicitly modeled (WorkItemById) or
    // conventional (GetWorkItemSummaryById) - carries a simple name ending in "ById"; no collection query does.
    // A legacy alias's own simple name is "StageLegacy", not the query's name, so this reuses AliasesOf - the
    // same lookup that already knows how to see past that suffix to the name underneath - rather than a second,
    // narrower way of reading the same identity.
    static bool IsByIdLookup(EndpointCandidate candidate) =>
        candidate.Names.SelectMany(AliasesOf).Any(alias => alias.EndsWith("ById", StringComparison.Ordinal));

    static List<EndpointCandidate> Candidates(EndpointDataSource endpoints)
    {
        var candidates = new List<EndpointCandidate>();

        foreach (var endpoint in endpoints.Endpoints.OfType<RouteEndpoint>())
        {
            var pattern = endpoint.RoutePattern.RawText;
            if (string.IsNullOrEmpty(pattern))
            {
                continue;
            }

            // A command registers an execute route and a validate route against the same type. The validate
            // route is a companion, never what an action posts to.
            if (pattern.EndsWith("/validate", StringComparison.Ordinal))
            {
                continue;
            }

            var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
            var method = "GET";
            if (methods.Contains("POST"))
            {
                method = "POST";
            }
            else if (methods.Count > 0)
            {
                method = methods[0];
            }

            var names = NamesOf(endpoint).ToList();
            if (names.Count == 0)
            {
                continue;
            }

            candidates.Add(new EndpointCandidate($"/{pattern.TrimStart('/')}", method, names));
        }

        return candidates;
    }

    static IEnumerable<string> NamesOf(Endpoint endpoint)
    {
        foreach (var metadata in endpoint.Metadata)
        {
            switch (metadata)
            {
                case Type type when type.FullName is { Length: > 0 } full:
                    yield return full;
                    break;
                case IEndpointNameMetadata named when named.EndpointName is { Length: > 0 } name:
                    yield return name;
                    break;
                case IRouteNameMetadata routed when routed.RouteName is { Length: > 0 } routeName:
                    yield return routeName;
                    break;
            }
        }

        if (endpoint.DisplayName is { Length: > 0 } displayName)
        {
            yield return displayName;
        }
    }

    sealed record EndpointCandidate(string Route, string Method, IReadOnlyList<string> Names);
}
