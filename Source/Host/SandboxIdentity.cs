// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Files;
using Microsoft.AspNetCore.Authentication;

namespace Cratis.Stage.Host;

internal static class SandboxIdentity
{
    internal static void UseSandboxIdentity(this WebApplication app, string modelPath)
    {
        if (!app.Services.GetRequiredService<SandboxIdentityOptions>().Enabled)
        {
            return;
        }

        var compiler = new PlayFileCompiler();
        var result = Directory.Exists(modelPath) ? compiler.CompileFolder(modelPath).Result : compiler.CompileApplication(modelPath).Result;
        var principal = result.Success ? SandboxPersona.Create(result.Value!, app.Logger) : null;
        Use(app, principal);
    }

    /// <summary>
    /// Removes forwarded identity headers before anything else in the pipeline sees them, then authenticates.
    /// Call first after building the application, ahead of static files and routing, so neither the
    /// authentication handler nor a proxied endpoint receives a client-supplied identity.
    /// </summary>
    /// <param name="app">The application to configure.</param>
    internal static void UseSandboxIdentityHeaders(this IApplicationBuilder app)
    {
        if (app.ApplicationServices.GetRequiredService<SandboxIdentityOptions>().Enabled)
        {
            app.Use(StripIdentityHeaders);
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

    internal static void Use(IApplicationBuilder app, ClaimsPrincipal? principal)
    {
        if (!app.ApplicationServices.GetRequiredService<SandboxIdentityOptions>().Enabled)
        {
            return;
        }

        app.Use(StripIdentityHeaders);

        app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), api =>
        {
            api.Use((context, next) =>
            {
                context.User = principal is null ? new ClaimsPrincipal(new ClaimsIdentity())
                    : new ClaimsPrincipal(principal.Identities.Select(identity => new ClaimsIdentity(identity)));

                return next(context);
            });
        });
    }

    static bool IsIdentityHeader(string name) => name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase);
}
