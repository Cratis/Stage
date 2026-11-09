// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cratis.Arc;
using Cratis.Arc.Identity;
using Cratis.Specifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpOverrides;

namespace Cratis.Stage.Host.for_SandboxActingUser.given;

public class a_started_host : Specification
{
    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    protected WebApplication _app = null!;
    protected HttpClient _client = null!;
    protected HttpResponseMessage _response = null!;
    private protected readonly StageUser _user = new("alice", ["Editor"], new ClientPrincipal
    {
        IdentityProvider = SandboxUsers.IdentityProvider,
        UserId = "alice",
        UserDetails = "Alice Example",
        UserRoles = ["editor"],
        Claims = [new ClientPrincipalClaim { typ = "department", val = "catalog" }]
    });
    protected virtual bool UsersEnabled => true;
    protected virtual bool HasUsers => true;
    protected virtual bool Production => false;
    private protected virtual StageHostMode Mode => StageHostMode.Sandbox;
    protected virtual bool AllowRemote => false;
    protected string _cookie = "alice";
    protected string? _principal;
    protected string? _principalId;
    protected string? _principalName;
    protected bool _authenticated;
    protected virtual bool Development => true;
    protected virtual string PathBase => "";
    protected virtual string? ForwardedOrigin => null;
    string Origin => ForwardedOrigin ?? _client.BaseAddress!.GetLeftPart(UriPartial.Authority);

    async Task Establish()
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = Production ? Environments.Production : Environments.Development });
        builder.Services.AddSingleton(new SandboxIdentityOptions(!UsersEnabled));
        builder.Services.AddSingleton(new SandboxUsersOptions(UsersEnabled, AllowRemote));
        builder.Services.AddSingleton(HasUsers ? new SandboxUsers([_user]) : SandboxUsers.None);
        ConfigureDevelopment(builder);
        if (UsersEnabled) builder.Services.Configure<ArcOptions>(options => options.TrustForwardedIdentityHeaders = true);
        builder.Services.AddAuthentication(MicrosoftIDentityPlatformAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, MicrosoftIDentityPlatformAuthHandler>(MicrosoftIDentityPlatformAuthHandler.SchemeName, _ => { });
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedPrefix;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
        _app = builder.Build();
        _app.UseSandboxIdentityHeaders();
        _app.UseForwardedHeaders();
        _app.UseSandboxAuthentication();
        _app.UseRouting();
        SandboxActingUser.Map(_app);
        _app.MapMethods("/identity", ["GET", "POST"], (HttpContext context) =>
        {
            _authenticated = context.User.Identity?.IsAuthenticated == true;
            _principal = context.Request.Headers["x-ms-client-principal"].FirstOrDefault();
            _principalId = context.Request.Headers["x-ms-client-principal-id"].FirstOrDefault();
            _principalName = context.Request.Headers["x-ms-client-principal-name"].FirstOrDefault();
            return Results.NoContent();
        });
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync();
        var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        _client = new HttpClient(new HttpClientHandler { UseCookies = false, CheckCertificateRevocationList = true }) { BaseAddress = new Uri($"{addresses.Addresses.Single()}/") };
    }

    protected async Task SendIdentity(HttpMethod? method = null, string? origin = null, string? explicitHeader = null, bool webSocket = false, string? host = null, string? referer = null)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Get, "identity");
        request.Headers.Add("Cookie", $"stage-acting-user={_cookie}");
        Forward(request);
        if (origin is not null) request.Headers.Add("Origin", origin == "self" ? Origin : origin);
        if (referer is not null) request.Headers.Add("Referer", referer == "self" ? $"{Origin}{PathBase}/page" : referer);
        if (explicitHeader is not null) request.Headers.Add("x-ms-client-principal-id", explicitHeader);
        if (host is not null) request.Headers.Host = host;
        if (webSocket)
        {
            request.Headers.Add("Connection", "Upgrade");
            request.Headers.Add("Upgrade", "websocket");
        }
        _response = await _client.SendAsync(request);
    }

    protected async Task SendSelectionBody(string body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, SandboxActingUser.Route.TrimStart('/'))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        Forward(request);
        request.Headers.Add("Origin", Origin);
        _response = await _client.SendAsync(request);
    }

    protected async Task Switch(string? name = "alice", string? origin = "self", string? site = "same-origin", bool json = true, HttpMethod? method = null, string? host = null)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Put, SandboxActingUser.Route.TrimStart('/'));
        request.Headers.Add("Cookie", "stage-acting-user=alice");
        request.Content = json ? JsonContent.Create(new { name }) : new StringContent("name=alice");
        Forward(request);
        if (origin is not null) request.Headers.Add("Origin", origin == "self" ? Origin : origin);
        if (site is not null) request.Headers.Add("Sec-Fetch-Site", site);
        if (host is not null) request.Headers.Host = host;
        _response = await _client.SendAsync(request);
    }

    protected virtual void ConfigureDevelopment(WebApplicationBuilder builder) => builder.Services.AddSingleton(new SandboxActingUserAccess(Mode, Production, Development));

    void Forward(HttpRequestMessage request)
    {
        if (PathBase.Length > 0) request.Headers.Add("X-Forwarded-Prefix", PathBase);
        if (ForwardedOrigin is not null)
        {
            var origin = new Uri(ForwardedOrigin);
            request.Headers.Add("X-Forwarded-Host", origin.Authority);
            request.Headers.Add("X-Forwarded-Proto", origin.Scheme);
        }
    }

    protected ClientPrincipal MappedPrincipal() => JsonSerializer.Deserialize<ClientPrincipal>(Encoding.UTF8.GetString(Convert.FromBase64String(_principal!)), _json)!;
    protected bool HasCookie => _response.Headers.Contains("Set-Cookie");
    protected string Cookie => _response.Headers.GetValues("Set-Cookie").Single();

    async Task Destroy()
    {
        _response?.Dispose();
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
