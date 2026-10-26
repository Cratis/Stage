// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Claims;
using Cratis.Arc.Identity;
using Cratis.Specifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using NSubstitute;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_discovering_outside_development.given;

public class a_sandbox_host : Specification
{
    protected virtual string Environment => "Docker";
    WebApplication _app = null!;
    HttpClient _client = null!;

    async Task Establish()
    {
        // A content root without the host's own appsettings keeps the Docker listener (9090) out of the spec.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environment, ContentRootPath = Path.GetTempPath() });
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        SandboxIdentityOptions.Configure(builder.Services, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Stage:Sandbox:Identity"] = "true" }).Build());
        builder.Services.AddAuthentication(MicrosoftIDentityPlatformAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, MicrosoftIDentityPlatformAuthHandler>(MicrosoftIDentityPlatformAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Substitute.For<IProvideIdentityDetails>());
        builder.AddCratisArc();
        _app = builder.Build();

        var persona = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "persona:Shopper"), new Claim(ClaimTypes.Name, "Ada"), new Claim(ClaimTypes.Role, "Shopper")],
            "stage-persona"));
        _app.UseSandboxIdentityHeaders();
        _app.UseRouting();
        SandboxIdentity.Use(_app, persona);
        _app.UseAuthorization();
        _app.MapIntrospectionEndpoints();
        _app.MapIdentityProvider();
        _app.MapGet("/api/probe", () => Results.Ok()).RequireAuthorization(policy => policy.RequireRole("Shopper"));
        _app.MapGet("/other/probe", () => Results.Ok()).RequireAuthorization(policy => policy.RequireRole("Shopper"));
        await _app.StartAsync();

        var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        _client = new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    async Task Destroy()
    {
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    protected async Task<HttpStatusCode> Get(string path)
    {
        using var response = await _client.GetAsync(path);
        return response.StatusCode;
    }
}
