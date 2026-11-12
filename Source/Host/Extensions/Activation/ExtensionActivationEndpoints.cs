// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Cratis.Stage.Contracts;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// The endpoints of activation: the browser tells Stage it reached a screen, and the Workbench reads where each backend is.
/// </summary>
public static class ExtensionActivationEndpoints
{
    /// <summary>
    /// Gets the route the Workbench reads the status of backends from.
    /// </summary>
    public const string StatusRoute = "/stage/extensions/activation";

    /// <summary>
    /// Gets the route the browser posts to when it reaches a screen, followed by the screen's route.
    /// </summary>
    public const string ScreensRoute = "/stage/extensions/activation/screens";

    /// <summary>
    /// Maps the endpoint the browser calls on navigation. It starts backends without waiting and answers at once.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    public static void MapScreens(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost($"{ScreensRoute}/{{**screen}}", (string? screen, ScreenActivation activation) =>
            Results.Json(new { Backends = activation.Reach("/" + (screen ?? string.Empty).Trim('/')) }, StageJson.Options, statusCode: StatusCodes.Status202Accepted));

    /// <summary>
    /// Maps the endpoints the Workbench reads.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    public static void MapTooling(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(StatusRoute, (ExtensionActivator activator) => Results.Json(new { Backends = activator.Status() }, StageJson.Options));
        endpoints.MapGet($"{StatusRoute}/view", (ExtensionActivator activator) => Results.Content(Render(activator.Status()), "text/html; charset=utf-8"));
    }

    /// <summary>
    /// Renders the status as a page that runs no script.
    /// </summary>
    /// <param name="statuses">The statuses.</param>
    /// <returns>The page.</returns>
    public static string Render(IReadOnlyList<ExtensionBackendStatus> statuses)
    {
        var html = new StringBuilder("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; style-src 'unsafe-inline'\">");
        html.Append("<title>Extension backends</title><style>body{font-family:system-ui,sans-serif;margin:2rem}table{border-collapse:collapse}td,th{border:1px solid #ccc;padding:.4rem .8rem;text-align:left}</style></head><body><h1>Extension backends</h1>");
        if (statuses.Count == 0)
        {
            html.Append("<p>This model uses no extension backends.</p>");
        }
        else
        {
            html.Append("<table><tr><th>System</th><th>Package</th><th>Tier</th><th>State</th><th>Activation</th><th>Last activation</th><th>In flight</th><th>Problem</th></tr>");
            foreach (var status in statuses)
            {
                html.Append("<tr>");
                foreach (var cell in new[] { status.System, status.Reference, status.Tier.ToString(), status.State.ToString(), status.Activation, status.LastActivation?.ToString("u", System.Globalization.CultureInfo.InvariantCulture) ?? "never", status.InFlight.ToString(System.Globalization.CultureInfo.InvariantCulture), status.Refusal })
                {
                    html.Append("<td>").Append(WebUtility.HtmlEncode(cell)).Append("</td>");
                }

                html.Append("</tr>");
            }

            html.Append("</table>");
        }

        return html.Append("</body></html>").ToString();
    }
}
