// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_stripping_before_authentication.given;

public class a_started_host : Specification
{
    protected WebApplication _app = null!;
    protected HttpClient _client = null!;
    protected AuthenticationRecording _recording = new();
    protected string[] _headersSeenByEndpoint = [];
    protected HttpResponseMessage _response = null!;
    protected virtual bool SandboxEnabled => true;

    async Task Establish()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Stage:Sandbox:Identity"] = SandboxEnabled.ToString() });
        SandboxIdentityOptions.Configure(builder.Services, builder.Configuration);
        builder.Services.AddSingleton(_recording);
        builder.Services.AddAuthentication(RecordingAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, RecordingAuthenticationHandler>(RecordingAuthenticationHandler.SchemeName, _ => { });
        _app = builder.Build();
        _app.UseSandboxIdentityHeaders();
        _app.UseRouting();
        _app.MapGet("/workbench/anything", (HttpContext context) =>
        {
            _headersSeenByEndpoint = [.. context.Request.Headers.Keys.Where(name => name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase))];
            return Results.Ok();
        });
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync();
        var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        _client = new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    protected async Task Send()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/workbench/anything");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL", "spoofed");
        request.Headers.Add("X-Ms-Client-Principal-Id", "spoofed-id");
        request.Headers.Add("x-ms-client-principal-name", "spoofed-name");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-IDP", "attacker");
        _response = await _client.SendAsync(request);
    }

    async Task Destroy()
    {
        _response?.Dispose();
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
