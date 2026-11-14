// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

public partial class CratisBackendApplicationScaffold
{
    static string Authentication(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        using Microsoft.AspNetCore.Authentication;
        using Microsoft.AspNetCore.Authentication.Cookies;
        using Microsoft.AspNetCore.Authentication.JwtBearer;
        using Microsoft.AspNetCore.Authentication.OpenIdConnect;
        using Microsoft.AspNetCore.Authorization;
        using Microsoft.Identity.Web;

        namespace {{request.RootNamespace}};

        internal static partial class GeneratedHostAuthentication
        {
            const string Selector = "GeneratedHost";
            const string Cookie = "GeneratedHostCookie";
            const string Oidc = "GeneratedHostOidc";
            const string Bearer = "GeneratedHostBearer";

            internal static void ConfigureServices(WebApplicationBuilder builder)
            {
                var settings = ReadSettings(builder.Configuration);
                if (settings is null)
                {
                    return;
                }

                builder.Services.AddSingleton(settings);
                var authentication = builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = Selector;
                    options.DefaultAuthenticateScheme = Selector;
                    options.DefaultChallengeScheme = Selector;
                    options.DefaultForbidScheme = Selector;
                }).AddPolicyScheme(Selector, null, options => options.ForwardDefaultSelector = context => HasAuthorization(context) ? Bearer : Cookie);

                authentication.AddMicrosoftIdentityWebApp(options =>
                {
                    options.Instance = settings.Instance;
                    options.TenantId = settings.TenantId;
                    options.ClientId = settings.ClientId;
                    options.ClientCredentials = [settings.Credential];
                }, openIdConnectScheme: Oidc, cookieScheme: Cookie)
                    .EnableTokenAcquisitionToCallDownstreamApi()
                    .AddInMemoryTokenCaches();
                authentication.AddMicrosoftIdentityWebApi(_ => { }, options =>
                {
                    options.Instance = settings.Instance;
                    options.TenantId = settings.TenantId;
                    options.ClientId = settings.ClientId;
                }, jwtBearerScheme: Bearer);

                builder.Services.PostConfigure<CookieAuthenticationOptions>(Cookie, options =>
                {
                    options.Cookie.Name = "__Host-Cratis";
                    options.Cookie.Domain = null;
                    options.Cookie.Path = "/";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = false;
                    options.Events.OnRedirectToLogin = context => Status(context.Response, StatusCodes.Status401Unauthorized);
                    options.Events.OnRedirectToAccessDenied = context => Status(context.Response, StatusCodes.Status403Forbidden);
                });
                builder.Services.PostConfigure<OpenIdConnectOptions>(Oidc, options => ConfigureOidc(options, settings));
                builder.Services.PostConfigure<JwtBearerOptions>(Bearer, options => ConfigureBearer(options, settings));
                builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder(Selector).RequireAuthenticatedUser().Build());
            }

            static void ConfigureOidc(OpenIdConnectOptions options, Settings settings)
            {
                options.SignInScheme = Cookie;
                options.CallbackPath = "/signin-oidc";
                options.RemoteSignOutPath = PathString.Empty;
                options.SignedOutCallbackPath = PathString.Empty;
                options.RequireHttpsMetadata = true;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = false;
                options.MapInboundClaims = false;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                var redirect = options.Events.OnRedirectToIdentityProvider;
                options.Events.OnRedirectToIdentityProvider = async context =>
                {
                    await redirect(context);
                    context.ProtocolMessage.RedirectUri = new Uri(settings.PublicOrigin, "/signin-oidc").AbsoluteUri;
                    context.Properties.RedirectUri = "/";
                };
                options.Events.OnRedirectToIdentityProviderForSignOut = context =>
                {
                    context.HandleResponse();
                    return Status(context.Response, StatusCodes.Status401Unauthorized);
                };
                options.Events.OnMessageReceived = context =>
                {
                    if (HasAuthorization(context.HttpContext))
                    {
                        context.HandleResponse();
                        return Status(context.Response, StatusCodes.Status401Unauthorized);
                    }

                    return Task.CompletedTask;
                };
                options.Events.OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    return Status(context.Response, StatusCodes.Status401Unauthorized);
                };
                options.Events.OnTicketReceived = context =>
                {
                    context.ReturnUri = "/";
                    return Task.CompletedTask;
                };
            }

            static void ConfigureBearer(JwtBearerOptions options, Settings settings)
            {
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidAudience = settings.Audience;
                options.TokenValidationParameters.ValidAudiences = [settings.Audience];
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.RequireSignedTokens = true;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                var validated = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async context =>
                {
                    await validated(context);
                    var principal = context.Principal!;
                    var tenants = principal.FindAll("tid").ToArray();
                    var scopes = principal.FindAll("scp").ToArray();
                    var admitted = scopes.Length > 0
                        ? scopes.SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Contains("access_as_user", StringComparer.Ordinal)
                        : principal.HasClaim("idtyp", "app") && principal.FindAll("roles").Any(claim => !string.IsNullOrWhiteSpace(claim.Value));
                    var validTenant = tenants.Length == 1 && string.Equals(tenants[0].Value, settings.TenantId, StringComparison.OrdinalIgnoreCase);
                    if (!validTenant || !admitted)
                    {
                        context.Fail("The bearer credential is not admitted by this host.");
                    }
                };
            }

            static Task Status(HttpResponse response, int status)
            {
                response.StatusCode = status;
                return Task.CompletedTask;
            }
        }
        """;
}
