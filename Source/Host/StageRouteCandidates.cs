// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Api;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Stage.Host;

/// <summary>
/// The endpoints Arc registered, read as operations with an identity, and the rules for finding the one a scene
/// element or a name refers to.
/// </summary>
/// <remarks>
/// <para>
/// Every operation is matched by its identity - the endpoint name Arc gives it, such as
/// <c language="csharp">Workspaces.Tracking.WorkItemList.WorkItemSummary.AllWorkItems</c> - never by a substring of
/// it. Two queries over the same read model share everything up to their own name, so a substring match on the
/// read model finds both, and choosing between them by route length is a guess: that is how a list bound to
/// <c language="csharp">AllWorkItems</c> once ended up calling the single-item <c language="csharp">WorkItemById</c>
/// route with no argument and showed nothing.
/// </para>
/// <para>
/// A legacy alias (<c language="csharp">….StageLegacy</c>) is the same operation as its canonical route, so the two
/// share an identity and are never ambiguous with each other. Within one operation the shorter route wins, as it
/// always has.
/// </para>
/// </remarks>
internal static class StageRouteCandidates
{
    const string LegacySuffix = ".StageLegacy";

    /// <summary>
    /// Reads the routable operations from the registered endpoints.
    /// </summary>
    /// <param name="endpoints">The registered endpoints.</param>
    /// <returns>The candidates.</returns>
    internal static List<StageRouteCandidate> From(EndpointDataSource endpoints)
    {
        var candidates = new List<StageRouteCandidate>();

        foreach (var endpoint in endpoints.Endpoints.OfType<RouteEndpoint>())
        {
            var pattern = endpoint.RoutePattern.RawText;

            // A command registers an execute route and a validate route against the same type. The validate
            // route is a companion, never what an action posts to.
            if (string.IsNullOrEmpty(pattern) || pattern.EndsWith("/validate", StringComparison.Ordinal))
            {
                continue;
            }

            var names = NamesOf(endpoint).ToList();
            if (names.Count == 0)
            {
                continue;
            }

            candidates.Add(new StageRouteCandidate($"/{pattern.TrimStart('/')}", MethodOf(endpoint), IdentityOf(endpoint), names));
        }

        return candidates;
    }

    /// <summary>
    /// Finds the single operation an identity predicate selects for a verb.
    /// </summary>
    /// <param name="candidates">The candidates.</param>
    /// <param name="method">The verb.</param>
    /// <param name="selects">Whether an operation identity is the one asked for.</param>
    /// <returns>The resolution.</returns>
    internal static StageRouteResolution Resolve(IEnumerable<StageRouteCandidate> candidates, string method, Func<string, bool> selects)
    {
        var operations = candidates
            .Where(candidate => candidate.Identity is not null &&
                string.Equals(candidate.Method, method, StringComparison.Ordinal) &&
                selects(candidate.Identity))
            .GroupBy(candidate => candidate.Identity!, StringComparer.Ordinal)
            .ToArray();

        return operations.Length switch
        {
            0 => StageRouteResolution.Unresolved,
            1 => StageRouteResolution.Resolved(operations[0].OrderBy(candidate => candidate.Route.Length).First()),
            _ => StageRouteResolution.Ambiguous([.. operations.Select(operation => operation.Key).Order(StringComparer.Ordinal)])
        };
    }

    /// <summary>
    /// Gets the simple name of an operation identity - the query or command name.
    /// </summary>
    /// <param name="identity">The identity.</param>
    /// <returns>The simple name.</returns>
    internal static string SimpleName(string identity) => identity[(LastSeparator(identity) + 1)..];

    /// <summary>
    /// Gets whether an identity is an operation owned by the given type - its full name, or a name ending in it.
    /// </summary>
    /// <param name="identity">The operation identity.</param>
    /// <param name="typeName">The type name.</param>
    /// <returns><see langword="true"/> when the type owns the operation.</returns>
    internal static bool IsOwnedBy(string identity, string typeName)
    {
        var separator = LastSeparator(identity);
        if (separator <= 0)
        {
            return false;
        }

        var owner = identity[..separator];

        return string.Equals(owner, typeName, StringComparison.Ordinal) ||
            owner.EndsWith($".{typeName}", StringComparison.Ordinal) ||
            owner.EndsWith($"+{typeName}", StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets whether an identity is the type itself, or a name ending in it.
    /// </summary>
    /// <param name="identity">The operation identity.</param>
    /// <param name="typeName">The type name.</param>
    /// <returns><see langword="true"/> when the identity names the type.</returns>
    internal static bool Names(string identity, string typeName) =>
        string.Equals(identity, typeName, StringComparison.Ordinal) ||
        identity.EndsWith($".{typeName}", StringComparison.Ordinal) ||
        identity.EndsWith($"+{typeName}", StringComparison.Ordinal);

    /// <summary>
    /// Gets the conventional collection query name for a read model type name.
    /// </summary>
    /// <param name="typeName">The read model type name, simple or qualified.</param>
    /// <returns>The conventional collection query name.</returns>
    internal static string ConventionalCollection(string typeName) => $"All{ModelNaming.Pluralize(SimpleName(typeName))}";

    /// <summary>
    /// Gets every name an endpoint is known by, for diagnostics.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <returns>The names.</returns>
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

    static string? IdentityOf(Endpoint endpoint)
    {
        if (endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName is not { Length: > 0 } name)
        {
            return null;
        }

        return name.EndsWith(LegacySuffix, StringComparison.Ordinal) ? name[..^LegacySuffix.Length] : name;
    }

    static string MethodOf(Endpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
        if (methods.Contains("POST"))
        {
            return "POST";
        }

        return methods.Count > 0 ? methods[0] : "GET";
    }

    static int LastSeparator(string name) => Math.Max(name.LastIndexOf('.'), name.LastIndexOf('+'));
}

/// <summary>
/// A routable operation.
/// </summary>
/// <param name="Route">The route.</param>
/// <param name="Method">The verb.</param>
/// <param name="Identity">The operation identity, shared by a canonical route and its legacy alias; <see langword="null"/> for an unnamed endpoint.</param>
/// <param name="Names">Every name the endpoint is known by.</param>
internal sealed record StageRouteCandidate(string Route, string Method, string? Identity, IReadOnlyList<string> Names);

/// <summary>
/// The outcome of finding the operation a scene element or name refers to.
/// </summary>
/// <param name="Match">The operation, when exactly one was found.</param>
/// <param name="Candidates">The distinct operations that matched, when more than one did.</param>
internal sealed record StageRouteResolution(StageRouteCandidate? Match, IReadOnlyList<string> Candidates)
{
    /// <summary>
    /// Gets the resolution for nothing found.
    /// </summary>
    internal static StageRouteResolution Unresolved { get; } = new(null, []);

    /// <summary>
    /// Gets whether more than one distinct operation matched.
    /// </summary>
    internal bool IsAmbiguous => Candidates.Count > 1;

    /// <summary>
    /// Creates a resolution for exactly one operation.
    /// </summary>
    /// <param name="match">The operation.</param>
    /// <returns>The resolution.</returns>
    internal static StageRouteResolution Resolved(StageRouteCandidate match) => new(match, []);

    /// <summary>
    /// Creates a resolution for several distinct operations.
    /// </summary>
    /// <param name="candidates">The operation identities.</param>
    /// <returns>The resolution.</returns>
    internal static StageRouteResolution Ambiguous(IReadOnlyList<string> candidates) => new(null, candidates);
}
