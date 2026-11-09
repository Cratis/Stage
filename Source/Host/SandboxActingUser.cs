// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Stage.Host;

/// <summary>
/// A browser's modeled caller, available only in a development play sandbox with modeled users enabled.
/// </summary>
internal static class SandboxActingUser
{
    internal const string Route = "/stage/sandbox/acting-user";
    const string CookieName = "stage-acting-user";
    static readonly object _localRequest = new();
    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    internal static bool Enabled(IServiceProvider services) => services.GetService<SandboxActingUserAccess>() is { Mode: StageHostMode.Sandbox, Production: false, Development: true }
        && services.GetService<SandboxUsersOptions>() is { Enabled: true }
        && services.GetService<SandboxUsers>() is { All.Count: > 0 };

    internal static void Map(WebApplication app)
    {
        if (!Enabled(app.Services)) return;
        app.MapGet(Route, (HttpContext context) => Available(context) ? Results.Json(State(context), _json) : Results.NotFound()).AllowAnonymous();
        app.MapPut(Route, (Delegate)Select).AllowAnonymous();
    }

    // Capture the raw connection and Host before forwarded headers can change the request.
    internal static void CaptureLocalRequest(HttpContext context) => context.Items[_localRequest] = SandboxIdentity.IsLocalRequest(context);

    // Called after forwarded headers and before authentication, retaining the raw access decision.
    internal static void Apply(HttpContext context)
    {
        if (!Available(context) || !context.Request.Cookies.TryGetValue(CookieName, out var name))
        {
            return;
        }

        var user = Find(context, name);
        if (user is null)
        {
            Clear(context);
            return;
        }

        if (context.Request.Headers.Keys.Any(SandboxIdentity.IsIdentityHeader))
        {
            return;
        }

        var unsafeMethod = !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method);
        var webSocket = context.Request.Headers.Upgrade.Any(value => string.Equals(value, "websocket", StringComparison.OrdinalIgnoreCase));
        if ((unsafeMethod || webSocket) && !SameOrigin(context, allowReferer: true))
        {
            return;
        }

        // A cookie is only a modeled name. Roles and claims always come from the current model.
        context.Request.Headers["x-ms-client-principal"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(user.Principal, _json)));
        context.Request.Headers["x-ms-client-principal-id"] = user.Principal.UserId;
        context.Request.Headers["x-ms-client-principal-name"] = user.Principal.UserDetails;
        context.Request.Headers["x-ms-client-principal-idp"] = SandboxUsers.IdentityProvider;
    }

    static bool Available(HttpContext context)
    {
        return Enabled(context.RequestServices)
            && (context.RequestServices.GetRequiredService<SandboxUsersOptions>().AllowRemote || context.Items[_localRequest] is true);
    }

    static StageUser? Find(HttpContext context, string? name) => name is { Length: > 0 and <= 1024 }
        ? context.RequestServices.GetRequiredService<SandboxUsers>().All.FirstOrDefault(user => string.Equals(user.Name, name, StringComparison.Ordinal))
        : null;

    static object State(HttpContext context)
    {
        var users = context.RequestServices.GetRequiredService<SandboxUsers>().All;
        var selected = context.Request.Cookies.TryGetValue(CookieName, out var name) ? Find(context, name) : null;
        if (name is not null && selected is null) Clear(context);
        return new { enabled = true, users = users.Select(user => new { name = user.Name, displayName = user.Principal.UserDetails, personas = user.Personas }), current = selected?.Name };
    }

    static async Task<IResult> Select(HttpContext context)
    {
        if (!Available(context)) return Results.NotFound();
        if (!SameOrigin(context, allowReferer: false)) return Results.StatusCode(StatusCodes.Status403Forbidden);
        if (!context.Request.HasJsonContentType()) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
        }
        catch (JsonException)
        {
            return Results.BadRequest();
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("name", out var name)
                || name.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
            {
                return Results.BadRequest();
            }

            var selected = name.ValueKind == JsonValueKind.String ? Find(context, name.GetString()) : null;
            if (name.ValueKind == JsonValueKind.String && selected is null)
            {
                return Results.BadRequest();
            }

            if (selected is null)
            {
                Clear(context);
            }
            else
            {
                context.Response.Cookies.Append(CookieName, selected.Name, CookieOptions(context));
            }
            return Results.NoContent();
        }
    }

    static CookieOptions CookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Secure = context.Request.IsHttps,
        Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "/"
    };

    static void Clear(HttpContext context) => context.Response.Cookies.Delete(CookieName, CookieOptions(context));

    static bool SameOrigin(HttpContext context, bool allowReferer)
    {
        var site = context.Request.Headers["Sec-Fetch-Site"];
        if (site.Count > 0 && (site.Count != 1 || site[0] != "same-origin")) return false;
        var origin = context.Request.Headers.Origin;
        if (origin.Count > 0) return origin.Count == 1 && MatchesOrigin(context, origin[0], originOnly: true);
        var referer = context.Request.Headers.Referer;
        return allowReferer && referer.Count == 1 && MatchesOrigin(context, referer[0], originOnly: false);
    }

    static bool MatchesOrigin(HttpContext context, string? value, bool originOnly) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment)
        && (!originOnly || (uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query)))
        && Uri.TryCreate($"{context.Request.Scheme}://{context.Request.Host}", UriKind.Absolute, out var expected)
        && uri.Scheme == expected.Scheme && uri.Authority.Equals(expected.Authority, StringComparison.OrdinalIgnoreCase);
}
