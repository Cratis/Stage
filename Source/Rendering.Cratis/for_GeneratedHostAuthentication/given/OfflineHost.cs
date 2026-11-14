// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.WebSockets;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

sealed class OfflineHost : IAsyncDisposable
{
    internal const string CookieScheme = "GeneratedHostCookie";
    internal const string BearerScheme = "GeneratedHostBearer";
    internal const string OidcScheme = "GeneratedHostOidc";
    readonly WebApplication _app;
    readonly HttpClient _client;
    readonly HttpClient _backchannel;
    readonly OfflineAuthenticationFixture _fixture;
    bool _disposed;

    OfflineHost(WebApplication app, OfflineAuthenticationFixture fixture, HttpClient backchannel)
    {
        _app = app;
        _fixture = fixture;
        _backchannel = backchannel;
        Issuer = fixture.Issuer;
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        _client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, CheckCertificateRevocationList = true })
        {
            BaseAddress = new Uri(address),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    internal OfflineIssuer Issuer { get; }
    internal IServiceProvider Services => _app.Services;
    internal OfflineAuthenticationDiagnostics Diagnostics => _fixture.Diagnostics;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await OfflineHostTeardown.Release(
            _fixture,
            _app.StopAsync,
            _app.DisposeAsync,
            () => Dispose(true));
    }

    internal static Dictionary<string, string?> EnabledSettings() => new()
    {
        ["Stage:Authentication:EntraId:Enabled"] = "true",
        ["Stage:Authentication:PublicOrigin"] = OfflineIssuer.Origin,
        ["Stage:Authentication:EntraId:TenantId"] = OfflineIssuer.Tenant,
        ["Stage:Authentication:EntraId:ClientId"] = OfflineIssuer.Client,
        ["Stage:Authentication:EntraId:Audience"] = OfflineIssuer.Audience,
        ["Stage:Authentication:EntraId:ClientCredentials:0:SourceType"] = "SignedAssertionFromManagedIdentity"
    };

    internal static async Task<OfflineHost> Start(OfflineAuthenticationFixture fixture, Dictionary<string, string?>? settings = null)
    {
        fixture.BorrowHost();
        var app = (WebApplication?)null;
        var backchannel = (HttpClient?)null;
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], ContentRootPath = AppContext.BaseDirectory });
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new OfflineCredentialEventLoggerProvider(fixture.Diagnostics));
            builder.Logging.AddFilter<OfflineCredentialEventLoggerProvider>(OfflineCredentialEventLoggerProvider.Category, LogLevel.Information);
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(settings ?? EnabledSettings());
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");

            // Model the earlier AddCratis default; emitted registration must override all selectors afterward.
            builder.Services.AddAuthentication(options => options.DefaultScheme = "Cratis").AddCookie("Cratis");
            builder.Services.AddAuthorization();
            EmittedAuthentication.Services(builder);
            backchannel = ((IHttpClientFactory)fixture.Transport).CreateClient("oidc");
            OfflineMetadata.Configure(builder, fixture, backchannel);
            app = builder.Build();
            var schemes = app.Services.GetRequiredService<IAuthenticationSchemeProvider>();
            if (await schemes.GetSchemeAsync(OidcScheme) is not null)
            {
                fixture.AssertBindings(app.Services);
            }

            EmittedAuthentication.Application(app);
            app.UseStaticFiles(new StaticFileOptions { FileProvider = new OfflineStaticFiles() });
            MapEndpoints(app);
            await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(10));
            return new OfflineHost(app, fixture, backchannel);
        }
        catch (Exception exception)
        {
            var failure = fixture.Diagnostics.Reject(exception);
            await OfflineHostTeardown.Release(
                fixture,
                token => app?.StopAsync(token) ?? Task.CompletedTask,
                () => app?.DisposeAsync() ?? ValueTask.CompletedTask,
                () => backchannel?.Dispose());
            throw failure;
        }
    }

    internal async Task<HttpResponseMessage> Send(string path, HostRequest? options = null)
    {
        options ??= new HostRequest();
        using var request = new HttpRequestMessage(new HttpMethod(options.Method), path);
        if (options.Cookie is not null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", options.Cookie);
        }

        if (options.Authorization is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", options.Authorization);
        }

        if (options.Origin is not null)
        {
            request.Headers.TryAddWithoutValidation("Origin", options.Origin);
        }

        if (options.Navigate)
        {
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
            request.Headers.Host = "untrusted.example.test";
            request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "forwarded.example.test");
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "http");
        }

        return await SendRequest(request);
    }

    internal string SessionCookie()
    {
        // A real protected ticket, not a principal assigned to HttpContext. This isolates cookie-handler/perimeter
        // behavior; it deliberately does not claim to prove OIDC authorization-code redemption.
        var options = Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieScheme);
        var identity = new ClaimsIdentity([new Claim("sub", "offline-browser")], CookieScheme);
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            },
            CookieScheme);
        return $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}";
    }

    internal async Task<HttpResponseMessage> Callback(IEnumerable<KeyValuePair<string, string>> form, string cookies)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/signin-oidc");
        request.Headers.TryAddWithoutValidation("Cookie", cookies);
        request.Content = new FormUrlEncodedContent(form);
        Diagnostics.Observe(OfflineAuthenticationPhase.CallbackDispatched);
        return await SendRequest(request);
    }

    internal async Task<HttpStatusCode> Upgrade(HostRequest request)
    {
        using var socket = new ClientWebSocket();
        socket.Options.CollectHttpResponseDetails = true;
        if (request.Cookie is not null)
        {
            socket.Options.SetRequestHeader("Cookie", request.Cookie);
        }

        if (request.Origin is not null)
        {
            socket.Options.SetRequestHeader("Origin", request.Origin);
        }

        if (request.Authorization is not null)
        {
            socket.Options.SetRequestHeader("Authorization", request.Authorization);
        }

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _fixture.BeginRequest();
        try
        {
            await socket.ConnectAsync(new UriBuilder(_client.BaseAddress!) { Scheme = "ws", Path = "/socket" }.Uri, cancellation.Token);
        }
        catch (WebSocketException) when (socket.HttpStatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            return socket.HttpStatusCode;
        }
        catch (Exception exception)
        {
            throw Diagnostics.Reject(exception);
        }
        finally
        {
            _fixture.EndRequest();
        }

        return socket.HttpStatusCode;
    }

    async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request)
    {
        _fixture.BeginRequest();
        try
        {
            return await _client.SendAsync(request);
        }
        catch (Exception exception)
        {
            throw Diagnostics.Reject(exception);
        }
        finally
        {
            _fixture.EndRequest();
        }
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client.Dispose();
            _backchannel.Dispose();
        }
    }

    static void MapEndpoints(WebApplication app)
    {
        app.Map("/socket", async context =>
        {
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, context.RequestAborted);
        }).AllowAnonymous();

        // Exercise endpoint metadata and paths that used to be anonymous or terminate before authorization.
        foreach (var path in new[] { "/", "/api/command", "/api/query", "/.cratis", "/.cratis/discovery", "/healthz", "/swagger", "/index.html", "/customization", "/signin-oidc-extra", "/signin-oidc/nested", "/signout-oidc", "/signout-callback-oidc" })
        {
            app.MapMethods(
                path,
                ["GET", "HEAD", "OPTIONS", "POST", "PUT", "PATCH", "DELETE"],
                (HttpContext context) => Results.Text(context.User.Identity?.IsAuthenticated == true ? "authenticated" : "anonymous")).AllowAnonymous();
        }
    }
}
