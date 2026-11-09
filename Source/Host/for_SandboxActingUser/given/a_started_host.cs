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

    async Task Establish()
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = Production ? Environments.Production : Environments.Development });
        builder.Services.AddSingleton(new SandboxIdentityOptions(!UsersEnabled));
        builder.Services.AddSingleton(new SandboxUsersOptions(UsersEnabled, AllowRemote));
        builder.Services.AddSingleton(HasUsers ? new SandboxUsers([_user]) : SandboxUsers.None);
        builder.Services.AddSingleton(new SandboxActingUserAccess(Mode, Production, Development));
        if (UsersEnabled) builder.Services.Configure<ArcOptions>(options => options.TrustForwardedIdentityHeaders = true);
        builder.Services.AddAuthentication(MicrosoftIDentityPlatformAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, MicrosoftIDentityPlatformAuthHandler>(MicrosoftIDentityPlatformAuthHandler.SchemeName, _ => { });
        _app = builder.Build();
        if (PathBase.Length > 0) _app.UsePathBase(PathBase);
        _app.UseSandboxIdentityHeaders();
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
        _client = new HttpClient(new HttpClientHandler { UseCookies = false, CheckCertificateRevocationList = true }) { BaseAddress = new Uri($"{addresses.Addresses.Single()}{PathBase}/") };
    }

    protected async Task SendIdentity(HttpMethod? method = null, string? origin = null, string? explicitHeader = null, bool webSocket = false, string? host = null, string? referer = null)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Get, "identity");
        request.Headers.Add("Cookie", $"stage-acting-user={_cookie}");
        if (origin is not null) request.Headers.Add("Origin", origin == "self" ? _client.BaseAddress!.GetLeftPart(UriPartial.Authority) : origin);
        if (referer is not null) request.Headers.Add("Referer", referer == "self" ? new Uri(_client.BaseAddress!, "page").ToString() : referer);
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
        request.Headers.Add("Origin", _client.BaseAddress!.GetLeftPart(UriPartial.Authority));
        _response = await _client.SendAsync(request);
    }

    protected async Task Switch(string? name = "alice", string? origin = "self", string? site = "same-origin", bool json = true, HttpMethod? method = null, string? host = null)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Put, SandboxActingUser.Route.TrimStart('/'));
        request.Headers.Add("Cookie", "stage-acting-user=alice");
        request.Content = json ? JsonContent.Create(new { name }) : new StringContent("name=alice");
        if (origin is not null) request.Headers.Add("Origin", origin == "self" ? _client.BaseAddress!.GetLeftPart(UriPartial.Authority) : origin);
        if (site is not null) request.Headers.Add("Sec-Fetch-Site", site);
        if (host is not null) request.Headers.Host = host;
        _response = await _client.SendAsync(request);
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
