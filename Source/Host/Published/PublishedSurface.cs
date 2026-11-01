// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Routing;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// Knows which routes are tooling and must not exist in a published application.
/// </summary>
internal static class PublishedSurface
{
    /// <summary>
    /// The route prefixes of tooling: API reference, Workbench proxy, read-model browser, model loading, admission,
    /// specification runs and the package endpoints.
    /// </summary>
    internal static readonly string[] ToolingPrefixes =
    [
        "/scalar",
        "/openapi",
        "/workbench",
        "/read-models",
        "/stage/read-models",
        "/stage/load",
        "/stage/semantic",
        "/stage/packages"
    ];

    internal static bool IsTooling(string route)
    {
        var normalized = "/" + route.Trim('/');
        return ToolingPrefixes.Any(prefix =>
            normalized.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Lists the mapped endpoints that are tooling.
    /// </summary>
    /// <param name="endpoints">Every mapped endpoint of the application.</param>
    /// <returns>The offending route patterns.</returns>
    internal static IReadOnlyList<string> ToolingRoutes(IEnumerable<Endpoint> endpoints) =>
        [.. endpoints.OfType<RouteEndpoint>().Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty).Where(IsTooling).Distinct().Order(StringComparer.Ordinal)];

    /// <summary>
    /// Fails startup when a tooling route was mapped, whatever mapped it.
    /// </summary>
    /// <param name="app">The built application, with every endpoint mapped.</param>
    /// <exception cref="PublishedModeRefused">A tooling route is mapped.</exception>
    internal static void EnsureNoTooling(WebApplication app)
    {
        var routes = ToolingRoutes(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints));
        if (routes.Count > 0)
        {
            throw new PublishedModeRefused([$"Tooling routes are mapped: {string.Join(", ", routes)}."]);
        }
    }

    /// <summary>
    /// Answers 404 for tooling paths before static files and the single-page fallback can serve them.
    /// </summary>
    /// <param name="app">The application pipeline.</param>
    /// <returns>The same pipeline.</returns>
    internal static IApplicationBuilder UseToolingRefusal(this IApplicationBuilder app) => app.Use((context, next) =>
    {
        if (IsTooling(context.Request.Path.Value ?? string.Empty))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }

        return next(context);
    });
}
