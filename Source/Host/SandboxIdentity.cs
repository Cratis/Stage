// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Claims;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Microsoft.AspNetCore.Authentication;

namespace Cratis.Stage.Host;

internal static class SandboxIdentity
{
    internal static void UseSandboxIdentity(this WebApplication app, string modelPath, ExecutableSemanticModel? model = null, StageHttpSurface? surface = null)
    {
        if (!app.Services.GetRequiredService<SandboxIdentityOptions>().Enabled)
        {
            return;
        }

        var compiler = new PlayFileCompiler();
        var result = Directory.Exists(modelPath) ? compiler.CompileFolder(modelPath).Result : compiler.CompileApplication(modelPath).Result;
        if (result.Success && model is not null && surface is not null)
        {
            var personas = new SandboxPersonas(result.Value!, model, surface, app.Logger);
            UseForRequests(app, personas.For);
        }
        else
        {
            Use(app, result.Success ? SandboxPersona.Create(result.Value!, app.Logger) : null);
        }
    }

    /// <summary>
    /// Removes forwarded identity headers unless modeled users accept the caller's address and host, then authenticates.
    /// Call first after building the application, ahead of static files and routing, so the same connection
    /// decision applies to authentication and proxied endpoints.
    /// </summary>
    /// <param name="app">The application to configure.</param>
    internal static void UseSandboxIdentityHeaders(this IApplicationBuilder app)
    {
        var sandbox = app.ApplicationServices.GetRequiredService<SandboxIdentityOptions>();
        var users = app.ApplicationServices.GetRequiredService<SandboxUsersOptions>();
        if (sandbox.Enabled || users.Enabled)
        {
            app.Use((context, next) =>
            {
                return !users.Enabled || (!users.AllowRemote && !IsLocalRequest(context))
                    ? StripIdentityHeaders(context, next)
                    : next();
            });
        }

        if (SandboxActingUser.Enabled(app.ApplicationServices))
        {
            app.Use((context, next) =>
            {
                SandboxActingUser.Apply(context);
                return next(context);
            });
        }

        if (app.ApplicationServices.GetService<IAuthenticationSchemeProvider>() is not null)
        {
            app.UseAuthentication();
        }
    }

    internal static Task StripIdentityHeaders(HttpContext context, Func<Task> next)
    {
        foreach (var name in context.Request.Headers.Keys.Where(IsIdentityHeader).ToArray())
        {
            context.Request.Headers.Remove(name);
        }

        return next();
    }

    internal static void Use(IApplicationBuilder app, ClaimsPrincipal? principal) => UseForRequests(app, _ => principal);

    internal static bool IsLocalRequest(HttpContext context) => IsLoopback(context.Connection.RemoteIpAddress)
        && (string.Equals(context.Request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Request.Host.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Request.Host.Host, "[::1]", StringComparison.OrdinalIgnoreCase));

    internal static bool IsIdentityHeader(string name) => name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase);

    static void UseForRequests(IApplicationBuilder app, Func<HttpContext, ClaimsPrincipal?> select)
    {
        if (!app.ApplicationServices.GetRequiredService<SandboxIdentityOptions>().Enabled)
        {
            return;
        }

        app.Use(StripIdentityHeaders);

        // The identity of the page itself (`/.cratis/me`, which Arc asks as soon as the page loads) is the persona being played, as it is for the
        // `/api` requests: without one it answers 401 on every start of a Play session.
        app.UseWhen(context => context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/.cratis/me"), api =>
        {
            api.Use((context, next) =>
            {
                var principal = select(context);
                context.User = principal is null ? new ClaimsPrincipal(new ClaimsIdentity())
                    : new ClaimsPrincipal(principal.Identities.Select(identity => new ClaimsIdentity(identity)));

                return next(context);
            });
        });
    }

    static bool IsLoopback(IPAddress? address) => address is not null && IPAddress.IsLoopback(address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address);
}
