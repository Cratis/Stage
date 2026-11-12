// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// The endpoints an extension component in the browser uses: its public settings, and invoking an ability.
/// </summary>
public static class ExtensionComponentEndpoints
{
    /// <summary>
    /// Gets the path prefix: <c lang="text">/stage/extension-components/&lt;Screen&gt;/&lt;System&gt;/&lt;Component&gt;</c>.
    /// </summary>
    public const string Prefix = "/stage/extension-components";

    /// <summary>
    /// Maps the endpoints.
    /// </summary>
    /// <param name="app">The application.</param>
    public static void Map(WebApplication app)
    {
        app.MapGet($"{Prefix}/{{screen}}/{{system}}/{{component}}/settings", (string screen, string system, string component, IExtensionComponentCatalog components, ExtensionPublicSettings settings) =>
            components.Find(screen, system, component) is null
                ? Results.NotFound()
                : Results.Json(new { Settings = settings.Of(system) }, StageJson.Options));

        app.MapPost($"{Prefix}/{{screen}}/{{system}}/{{component}}/invoke", async (
            string screen, string system, string component, InvokeBody body, HttpContext http, BrowserInvocations invocations) =>
        {
            var result = await invocations.Invoke(
                new BrowserInvocation(screen, system, component, body.Ability ?? string.Empty, body.Interaction ?? string.Empty, body.Input ?? "{}"),
                http.User,
                http.RequestAborted);
            var shape = new { Status = result.Status.ToString(), result.Identity, result.Outcome, result.Output, result.Reason };
            return result.Status switch
            {
                BrowserInvocationStatus.NotPlaced => Results.Json(shape, StageJson.Options, statusCode: StatusCodes.Status404NotFound),
                BrowserInvocationStatus.NotAllowed => Results.Json(shape, StageJson.Options, statusCode: StatusCodes.Status403Forbidden),
                BrowserInvocationStatus.Forbidden => Results.Json(shape, StageJson.Options, statusCode: http.User.Identity?.IsAuthenticated == true ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized),
                BrowserInvocationStatus.Refused => Results.Json(shape, StageJson.Options, statusCode: StatusCodes.Status400BadRequest),
                BrowserInvocationStatus.Pending => Results.Json(shape, StageJson.Options, statusCode: StatusCodes.Status202Accepted),
                _ => Results.Json(shape, StageJson.Options)
            };
        });
    }

    sealed record InvokeBody(string? Ability, string? Interaction, string? Input);
}
