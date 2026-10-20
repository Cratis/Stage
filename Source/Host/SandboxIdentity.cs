// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Files;

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

    internal static void Use(IApplicationBuilder app, ClaimsPrincipal? principal)
    {
        if (!app.ApplicationServices.GetRequiredService<SandboxIdentityOptions>().Enabled)
        {
            return;
        }

        app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), api =>
        {
            api.Use((context, next) =>
            {
                context.Request.Headers.Remove("x-ms-client-principal");
                context.Request.Headers.Remove("x-ms-client-principal-id");
                context.Request.Headers.Remove("x-ms-client-principal-name");
                context.User = principal is null ? new ClaimsPrincipal(new ClaimsIdentity())
                    : new ClaimsPrincipal(principal.Identities.Select(identity => new ClaimsIdentity(identity)));

                return next(context);
            });
        });
    }
}
