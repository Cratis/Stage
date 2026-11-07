// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

namespace Cratis.Stage.Host.Published;

internal static class PublishedAuthentication
{
    internal const string Selector = "StagePublishedAuthentication";
    internal const string Cookie = CookieAuthenticationDefaults.AuthenticationScheme;
    internal const string Oidc = OpenIdConnectDefaults.AuthenticationScheme;
    internal const string Bearer = JwtBearerDefaults.AuthenticationScheme;

    internal static void Configure(IServiceCollection services, PublishedOptions published)
    {
        published.EnsureComplete();
        var settings = published.Oidc!;
        var origin = new Uri(settings.PublicOrigin!);
        services.AddAuthorization();
        var authentication = services.AddAuthentication(Selector);
        authentication.AddMicrosoftIdentityWebApp(settings.EntraId, Oidc, Cookie)
            .EnableTokenAcquisitionToCallDownstreamApi().AddInMemoryTokenCaches();
        authentication.AddMicrosoftIdentityWebApi(settings.EntraId, Bearer);
        services.Configure<MicrosoftIdentityOptions>(Oidc, DefaultInstance);
        services.Configure<MicrosoftIdentityOptions>(Bearer, DefaultInstance);
        authentication.AddPolicyScheme(Selector, Selector, options => options.ForwardDefaultSelector = SelectScheme);
        services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = Selector;
            options.DefaultAuthenticateScheme = Selector;
            options.DefaultChallengeScheme = Selector;
            options.DefaultForbidScheme = Selector;
        });
        services.PostConfigure<CookieAuthenticationOptions>(Cookie, options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Events.OnRedirectToLogin = context => Status(context.HttpContext, StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context => Status(context.HttpContext, StatusCodes.Status403Forbidden);
        });
        services.PostConfigure<OpenIdConnectOptions>(Oidc, options =>
        {
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
            var signIn = options.Events.OnRedirectToIdentityProvider;
            options.Events.OnRedirectToIdentityProvider = async context =>
            {
                await signIn(context);
                context.ProtocolMessage.RedirectUri = new Uri(origin, options.CallbackPath.Value).AbsoluteUri;
            };
            var signOut = options.Events.OnRedirectToIdentityProviderForSignOut;
            options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
            {
                await signOut(context);
                context.ProtocolMessage.PostLogoutRedirectUri = new Uri(origin, options.SignedOutCallbackPath.Value).AbsoluteUri;
            };
        });
    }

    internal static string SelectScheme(HttpContext context) => context.Request.Headers.ContainsKey("Authorization") ? Bearer : Cookie;

    internal static void Use(IApplicationBuilder app, PublishedOptions published)
    {
        var origin = new Uri(published.Oidc!.PublicOrigin!);
        app.Use(StripIdentityHeaders);
        app.UseAuthentication();
        app.Use((context, next) => IsForeignOrigin(context.Request, origin) ? Status(context, StatusCodes.Status403Forbidden) : next());
        app.Use(Guard);
        app.UseAuthorization();
    }

    internal static Task StripIdentityHeaders(HttpContext context, Func<Task> next)
    {
        string[] prefixes = ["x-ms-client-principal", "x-stage-persona", "x-cratis-persona"];
        foreach (var name in context.Request.Headers.Keys.Where(name => prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))).ToArray())
        {
            context.Request.Headers.Remove(name);
        }
        return next();
    }

    static void DefaultInstance(MicrosoftIdentityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Instance))
        {
            options.Instance = "https://login.microsoftonline.com/";
        }
    }

    static bool IsForeignOrigin(HttpRequest request, Uri origin)
    {
        if (request.Headers.ContainsKey("Authorization"))
        {
            return false;
        }
        if (!IsUnsafe(request.Method) && !IsUpgrade(request))
        {
            return false;
        }
        var values = request.Headers.Origin;
        return values.Count != 1 || !Uri.TryCreate(values[0], UriKind.Absolute, out var actual) ||
            actual.Scheme != origin.Scheme || !string.Equals(actual.Host, origin.Host, StringComparison.OrdinalIgnoreCase) || actual.Port != origin.Port;
    }

    static bool IsUnsafe(string method) => !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method));
    static bool IsUpgrade(HttpRequest request) => request.Headers.Upgrade.Any(value => value?.Contains("websocket", StringComparison.OrdinalIgnoreCase) == true);

    static async Task Guard(HttpContext context, Func<Task> next)
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
            await context.ChallengeAsync(Oidc, new AuthenticationProperties { RedirectUri = ReturnUrl(request) });
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    }

    static bool IsHealthProbe(HttpRequest request) => IsPageMethod(request.Method) && request.Path.Equals("/healthz", StringComparison.OrdinalIgnoreCase);
    static bool IsPageMethod(string method) => HttpMethods.IsGet(method) || HttpMethods.IsHead(method);

    static bool IsNavigation(HttpRequest request)
    {
        if (!IsPageMethod(request.Method))
        {
            return false;
        }
        var mode = request.Headers["Sec-Fetch-Mode"];
        return mode.Count > 0 ? string.Equals(mode[0], "navigate", StringComparison.OrdinalIgnoreCase)
            : request.Headers.Accept.Any(value => value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
    }

    static string ReturnUrl(HttpRequest request)
    {
        var value = $"{request.PathBase}{request.Path}{request.QueryString}";
        return value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal) && !value.StartsWith("/\\", StringComparison.Ordinal) ? value : "/";
    }

    static Task Status(HttpContext context, int code)
    {
        context.Response.StatusCode = code;
        return Task.CompletedTask;
    }
}
