// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

public sealed partial class CratisBackendApplicationScaffold
{
    /// <summary>
    /// The one health route the emitted host maps; the authentication perimeter exempts exactly this route for GET and HEAD.
    /// </summary>
    internal const string HealthRoute = "/healthz";

    // Sign-in with Microsoft Entra ID is off unless configuration outside the managed artifacts turns it on. The
    // perimeter file depends on ASP.NET Core only; every Microsoft.Identity.Web call lives in the entry point file.
    static string AuthenticationPerimeter(CratisBackendApplicationScaffoldRequest request) =>
        string.Join(
            "\n\n",
            $$"""
            using Microsoft.AspNetCore.Authentication;
            using Microsoft.AspNetCore.Builder;
            using Microsoft.Extensions.Configuration;
            using System.Linq;

            namespace {{request.RootNamespace}}.GeneratedAuthentication;
            """,
            AuthenticationConfigurationFailure(),
            AuthenticationSettingsType(),
            AuthenticationPerimeterType());

    static string AuthenticationConfigurationFailure() =>
        """
        /// <summary>
        /// Thrown when the sign-in configuration is partial or invalid. The message names the configuration key and never its value.
        /// </summary>
        /// <param name="message">The failure message.</param>
        public sealed class InvalidAuthenticationConfiguration(string message) : global::System.Exception(message);
        """;

    static string AuthenticationSettingsType() =>
        """
        /// <summary>
        /// The validated sign-in settings. Sign-in is off when neither configuration section exists.
        /// </summary>
        /// <param name="PublicOrigin">The exact origin browsers use to reach the application.</param>
        public sealed record AuthenticationSettings(global::System.Uri PublicOrigin)
        {
            /// <summary>The configuration section holding the Microsoft Entra ID application registration.</summary>
            public const string EntraIdKey = "Cratis:Authentication:EntraId";

            /// <summary>The configuration key holding the public origin, such as scheme, host and optional port.</summary>
            public const string PublicOriginKey = "Cratis:Authentication:PublicOrigin";

            /// <summary>
            /// Reads the sign-in settings.
            /// </summary>
            /// <param name="configuration">The application configuration.</param>
            /// <returns>The settings, or null when sign-in is not configured.</returns>
            /// <exception cref="InvalidAuthenticationConfiguration">Thrown when the configuration is partial or invalid.</exception>
            public static AuthenticationSettings? Read(global::Microsoft.Extensions.Configuration.IConfiguration configuration)
            {
                var entra = configuration.GetSection(EntraIdKey);
                var origin = configuration.GetSection(PublicOriginKey);
                if (!entra.Exists() && !origin.Exists())
                {
                    return null;
                }

                if (!entra.Exists())
                {
                    throw Missing(EntraIdKey);
                }

                RequireValue(entra, "TenantId");
                RequireValue(entra, "ClientId");
                if (!entra.GetSection("ClientCredentials").GetChildren().Any())
                {
                    throw Missing($"{EntraIdKey}:ClientCredentials");
                }

                return new AuthenticationSettings(ReadOrigin(configuration[PublicOriginKey]));
            }

            static void RequireValue(global::Microsoft.Extensions.Configuration.IConfigurationSection section, string name)
            {
                if (string.IsNullOrWhiteSpace(section[name]))
                {
                    throw Missing($"{EntraIdKey}:{name}");
                }
            }

            static global::System.Uri ReadOrigin(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw Missing(PublicOriginKey);
                }

                if (!global::System.Uri.TryCreate(value, global::System.UriKind.Absolute, out var uri) || !IsOrigin(uri))
                {
                    throw new InvalidAuthenticationConfiguration($"The configuration key '{PublicOriginKey}' must be an https origin with only a scheme, host and optional port; http is allowed for loopback hosts only.");
                }

                return new global::System.Uri(uri.GetLeftPart(global::System.UriPartial.Authority));
            }

            static bool IsOrigin(global::System.Uri uri) =>
                (uri.Scheme == global::System.Uri.UriSchemeHttps || (uri.Scheme == global::System.Uri.UriSchemeHttp && uri.IsLoopback)) &&
                uri.Host.Length > 0 &&
                uri.UserInfo.Length == 0 &&
                uri.AbsolutePath == "/" &&
                uri.Query.Length == 0 &&
                uri.Fragment.Length == 0;

            static InvalidAuthenticationConfiguration Missing(string key) =>
                new($"The configuration key '{key}' is required when sign-in with Microsoft Entra ID is configured.");
        }
        """;

    static string AuthenticationPerimeterType() =>
        $$"""
        /// <summary>
        /// Protects every route, including the Arc routes and endpoints marked to allow anonymous access, once sign-in is configured.
        /// </summary>
        public static class AuthenticationPerimeter
        {
            /// <summary>The policy scheme that forwards each request to the cookie or the bearer scheme.</summary>
            public const string Selector = "CratisAuthenticationSelector";

            /// <summary>The cookie session scheme used by the browser application.</summary>
            public const string Cookie = global::Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;

            /// <summary>The OpenID Connect scheme that signs users in.</summary>
            public const string OpenIdConnect = global::Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme;

            /// <summary>The bearer token scheme used by service callers.</summary>
            public const string Bearer = global::Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;

            static readonly string[] _healthRoutes = ["{{HealthRoute}}"];

            /// <summary>
            /// Selects the scheme for a request: any Authorization header goes to bearer, everything else to the cookie. There is no fallback.
            /// </summary>
            /// <param name="context">The request.</param>
            /// <returns>The scheme name.</returns>
            public static string SelectScheme(global::Microsoft.AspNetCore.Http.HttpContext context) =>
                context.Request.Headers.ContainsKey("Authorization") ? Bearer : Cookie;

            /// <summary>
            /// Adds authentication, the origin check, the perimeter and authorization to the pipeline, in that order.
            /// </summary>
            /// <param name="app">The pipeline.</param>
            /// <param name="publicOrigin">The exact origin browsers use to reach the application.</param>
            public static void Use(global::Microsoft.AspNetCore.Builder.IApplicationBuilder app, global::System.Uri publicOrigin)
            {
                app.UseAuthentication();
                app.Use((context, next) => IsForeignOrigin(context.Request, publicOrigin) ? Forbid(context) : next());
                app.Use((context, next) => Guard(context, next));
                app.UseAuthorization();
            }

            static global::System.Threading.Tasks.Task Forbid(global::Microsoft.AspNetCore.Http.HttpContext context)
            {
                context.Response.StatusCode = global::Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden;
                return global::System.Threading.Tasks.Task.CompletedTask;
            }

            static bool IsForeignOrigin(global::Microsoft.AspNetCore.Http.HttpRequest request, global::System.Uri publicOrigin)
            {
                if (request.Headers.ContainsKey("Authorization") || !(IsUnsafe(request.Method) || IsUpgrade(request)))
                {
                    return false;
                }

                var origin = request.Headers.Origin;
                return origin.Count != 1 ||
                    !global::System.Uri.TryCreate(origin[0], global::System.UriKind.Absolute, out var uri) ||
                    uri.Scheme != publicOrigin.Scheme ||
                    !string.Equals(uri.Host, publicOrigin.Host, global::System.StringComparison.OrdinalIgnoreCase) ||
                    uri.Port != publicOrigin.Port;
            }

            static bool IsUnsafe(string method) =>
                !(global::Microsoft.AspNetCore.Http.HttpMethods.IsGet(method) ||
                  global::Microsoft.AspNetCore.Http.HttpMethods.IsHead(method) ||
                  global::Microsoft.AspNetCore.Http.HttpMethods.IsOptions(method) ||
                  global::Microsoft.AspNetCore.Http.HttpMethods.IsTrace(method));

            static bool IsUpgrade(global::Microsoft.AspNetCore.Http.HttpRequest request) =>
                request.Headers.Upgrade.Any(value => value is not null && value.Contains("websocket", global::System.StringComparison.OrdinalIgnoreCase));

            static async global::System.Threading.Tasks.Task Guard(global::Microsoft.AspNetCore.Http.HttpContext context, global::System.Func<global::System.Threading.Tasks.Task> next)
            {
                var request = context.Request;
                if (context.User.Identity?.IsAuthenticated == true || IsHealthProbe(request))
                {
                    await next();
                    return;
                }

                if (request.Headers.ContainsKey("Authorization"))
                {
                    await context.ChallengeAsync(Bearer);
                }
                else if (IsNavigation(request))
                {
                    await context.ChallengeAsync(OpenIdConnect, new global::Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = ReturnUrl(request) });
                }
                else
                {
                    context.Response.StatusCode = global::Microsoft.AspNetCore.Http.StatusCodes.Status401Unauthorized;
                }
            }

            static bool IsHealthProbe(global::Microsoft.AspNetCore.Http.HttpRequest request) =>
                (global::Microsoft.AspNetCore.Http.HttpMethods.IsGet(request.Method) || global::Microsoft.AspNetCore.Http.HttpMethods.IsHead(request.Method)) &&
                _healthRoutes.Contains(request.Path.Value, global::System.StringComparer.OrdinalIgnoreCase);

            static bool IsNavigation(global::Microsoft.AspNetCore.Http.HttpRequest request)
            {
                if (!(global::Microsoft.AspNetCore.Http.HttpMethods.IsGet(request.Method) || global::Microsoft.AspNetCore.Http.HttpMethods.IsHead(request.Method)))
                {
                    return false;
                }

                var mode = request.Headers["Sec-Fetch-Mode"];
                return mode.Count > 0
                    ? string.Equals(mode[0], "navigate", global::System.StringComparison.OrdinalIgnoreCase)
                    : request.Headers.Accept.Any(value => value is not null && value.Contains("text/html", global::System.StringComparison.OrdinalIgnoreCase));
            }

            static string ReturnUrl(global::Microsoft.AspNetCore.Http.HttpRequest request)
            {
                var value = $"{request.PathBase}{request.Path}{request.QueryString}";
                return value.StartsWith('/') && !value.StartsWith("//", global::System.StringComparison.Ordinal) && !value.StartsWith("/\\", global::System.StringComparison.Ordinal)
                    ? value
                    : "/";
            }
        }
        """;

    // The only file that names Microsoft.Identity.Web: it registers the cookie, OpenID Connect and bearer schemes
    // against the Cratis:Authentication:EntraId section, then pins what a deployment must not be able to loosen.
    static string Authentication(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Identity.Web;

        namespace {{request.RootNamespace}}.GeneratedAuthentication;

        /// <summary>
        /// Signs users in with Microsoft Entra ID when the Cratis:Authentication configuration sections exist; otherwise registers nothing.
        /// </summary>
        public static class Authentication
        {
            /// <summary>
            /// Registers sign-in services when configured.
            /// </summary>
            /// <param name="builder">The application builder.</param>
            public static void Add(global::Microsoft.AspNetCore.Builder.WebApplicationBuilder builder)
            {
                var settings = AuthenticationSettings.Read(builder.Configuration);
                if (settings is null)
                {
                    return;
                }

                builder.Services.AddAuthorization();
                var section = builder.Configuration.GetSection(AuthenticationSettings.EntraIdKey);
                var authentication = builder.Services.AddAuthentication(AuthenticationPerimeter.Selector);
                authentication
                    .AddMicrosoftIdentityWebApp(section, AuthenticationPerimeter.OpenIdConnect, AuthenticationPerimeter.Cookie)
                    .EnableTokenAcquisitionToCallDownstreamApi()
                    .AddInMemoryTokenCaches();
                authentication.AddMicrosoftIdentityWebApi(section, AuthenticationPerimeter.Bearer);
                authentication.AddPolicyScheme(
                    AuthenticationPerimeter.Selector,
                    AuthenticationPerimeter.Selector,
                    options => options.ForwardDefaultSelector = AuthenticationPerimeter.SelectScheme);
                PinSchemes(builder.Services);
                PinCookie(builder.Services);
                PinRedirects(builder.Services, settings.PublicOrigin);
            }

            /// <summary>
            /// Adds the authentication perimeter to the pipeline when sign-in is configured.
            /// </summary>
            /// <param name="app">The application.</param>
            public static void Use(global::Microsoft.AspNetCore.Builder.WebApplication app)
            {
                var settings = AuthenticationSettings.Read(app.Configuration);
                if (settings is not null)
                {
                    AuthenticationPerimeter.Use(app, settings.PublicOrigin);
                }
            }

            // Registered after the Identity.Web calls, so these run after their post-configuration.
            static void PinSchemes(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services) =>
                services.PostConfigure<global::Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
                {
                    options.DefaultScheme = AuthenticationPerimeter.Selector;
                    options.DefaultAuthenticateScheme = AuthenticationPerimeter.Selector;
                    options.DefaultChallengeScheme = AuthenticationPerimeter.Selector;
                    options.DefaultForbidScheme = AuthenticationPerimeter.Selector;
                });

            static void PinCookie(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services) =>
                services.PostConfigure<global::Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(AuthenticationPerimeter.Cookie, options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = global::Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                    options.Cookie.SameSite = global::Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                    options.Events.OnRedirectToLogin = context => Respond(context.Response, global::Microsoft.AspNetCore.Http.StatusCodes.Status401Unauthorized);
                    options.Events.OnRedirectToAccessDenied = context => Respond(context.Response, global::Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden);
                });

            // Redirect URIs come from the configured public origin only, never from the Host header.
            static void PinRedirects(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::System.Uri publicOrigin) =>
                services.PostConfigure<global::Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>(AuthenticationPerimeter.OpenIdConnect, options =>
                {
                    options.CorrelationCookie.SecurePolicy = global::Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                    options.NonceCookie.SecurePolicy = global::Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                    var signIn = options.Events.OnRedirectToIdentityProvider;
                    options.Events.OnRedirectToIdentityProvider = async context =>
                    {
                        await signIn(context);
                        context.ProtocolMessage.RedirectUri = new global::System.Uri(publicOrigin, options.CallbackPath.Value).AbsoluteUri;
                    };
                    var signOut = options.Events.OnRedirectToIdentityProviderForSignOut;
                    options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
                    {
                        await signOut(context);
                        context.ProtocolMessage.PostLogoutRedirectUri = new global::System.Uri(publicOrigin, options.SignedOutCallbackPath.Value).AbsoluteUri;
                    };
                });

            static global::System.Threading.Tasks.Task Respond(global::Microsoft.AspNetCore.Http.HttpResponse response, int statusCode)
            {
                response.StatusCode = statusCode;
                return global::System.Threading.Tasks.Task.CompletedTask;
            }
        }
        """;
}
