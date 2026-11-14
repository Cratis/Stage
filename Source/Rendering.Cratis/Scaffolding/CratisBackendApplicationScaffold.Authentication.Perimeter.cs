// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

public partial class CratisBackendApplicationScaffold
{
    static string AuthenticationPerimeter(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        using Microsoft.AspNetCore.Authentication;

        namespace {{request.RootNamespace}};

        internal static partial class GeneratedHostAuthentication
        {
            internal static void ConfigureApplication(WebApplication app)
            {
                var settings = app.Services.GetService<Settings>();
                if (settings is null)
                {
                    return;
                }

                app.UseRouting();
                app.UseWebSockets();
                app.UseAuthentication();
                app.Use((context, next) => Enforce(context, next, settings));
                app.UseAuthorization();
            }

            static async Task Enforce(HttpContext context, RequestDelegate next, Settings settings)
            {
                var authorization = HasAuthorization(context);
                var result = await context.AuthenticateAsync(authorization ? Bearer : Cookie);
                if (!result.Succeeded)
                {
                    if (!authorization && IsDocumentNavigation(context))
                    {
                        await context.ChallengeAsync(Oidc, new AuthenticationProperties { RedirectUri = "/" });
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    }

                    return;
                }

                var needsCookieOrigin = !authorization && NeedsOrigin(context);
                if (needsCookieOrigin && !HasSameOrigin(context, settings.PublicOrigin))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                context.User = result.Principal!;
                await next(context);
            }

            static bool HasAuthorization(HttpContext context) => context.Request.Headers.ContainsKey("Authorization");

            static bool IsDocumentNavigation(HttpContext context) =>
                HttpMethods.IsGet(context.Request.Method) && !context.WebSockets.IsWebSocketRequest &&
                !IsApiPath(context.Request.Path) && !IsProtocolPath(context.Request.Path) && HasNavigationHeaders(context);

            static bool IsApiPath(PathString path) => path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments("/.cratis", StringComparison.OrdinalIgnoreCase);

            static bool IsProtocolPath(PathString path) => path == "/signin-oidc" || path == "/signout-oidc" || path == "/signout-callback-oidc";

            static bool HasNavigationHeaders(HttpContext context) => context.Request.Headers["Sec-Fetch-Mode"] == "navigate" &&
                context.Request.Headers["Sec-Fetch-Dest"] == "document";

            static bool NeedsOrigin(HttpContext context) => context.WebSockets.IsWebSocketRequest || !IsSafeMethod(context.Request.Method);

            static bool IsSafeMethod(string method) => HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);

            static bool HasSameOrigin(HttpContext context, Uri expected)
            {
                var values = context.Request.Headers.Origin;
                if (values.Count != 1 || values[0] is not { } value)
                {
                    return false;
                }

                var malformed = value.Contains(',') || value.Any(char.IsWhiteSpace);
                if (malformed || !TryOrigin(value, out var actual))
                {
                    return false;
                }

                return actual.Scheme == expected.Scheme && string.Equals(actual.IdnHost, expected.IdnHost, StringComparison.OrdinalIgnoreCase) && actual.Port == expected.Port;
            }
        }
        """;
}
