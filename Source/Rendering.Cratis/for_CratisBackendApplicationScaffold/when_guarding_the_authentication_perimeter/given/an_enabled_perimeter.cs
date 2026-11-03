// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter.given;

public class an_enabled_perimeter : Specification
{
    protected const string FailureSentinel = "synthetic-health-exception-detail";
    protected WebApplication _app = null!;
    protected HttpClient _client = null!;
    protected HttpResponseMessage _response = null!;
    protected bool _unhealthy;
    protected string _body = string.Empty;
    protected string? _webSocketPath;
    protected System.Net.HttpStatusCode? _webSocketStatus;

    async Task Establish()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddAuthentication(the_emitted_perimeter.Selector)
            .AddScheme<AuthenticationSchemeOptions, stub_authentication>(the_emitted_perimeter.Cookie, _ => { })
            .AddScheme<AuthenticationSchemeOptions, stub_authentication>(the_emitted_perimeter.Bearer, _ => { })
            .AddScheme<AuthenticationSchemeOptions, stub_authentication>(the_emitted_perimeter.OpenIdConnect, _ => { })
            .AddScheme<AuthenticationSchemeOptions, stub_callback>("SyntheticCallback", _ => { })
            .AddPolicyScheme(the_emitted_perimeter.Selector, null, options => options.ForwardDefaultSelector = the_emitted_perimeter.SelectScheme);
        builder.Services.AddAuthorization();
        builder.Services.AddHealthChecks().AddCheck("synthetic-check", () => _unhealthy ? throw new Exception(FailureSentinel) : HealthCheckResult.Healthy());
        _app = builder.Build();
        _app.UseWebSockets();
        _app.Use(async (context, next) =>
        {
            // TestServer's WebSocket client throws on a non-101 response; observe the real response, not its exception text.
            if (context.Request.Path == "/socket")
            {
                _webSocketPath = context.Request.Path;
                await next(context);
                _webSocketStatus = (System.Net.HttpStatusCode)context.Response.StatusCode;
                return;
            }
            await next(context);
        });
        the_emitted_perimeter.Use(_app);
        _app.MapHealthChecks("/healthz");
        _app.MapGet("/anonymous", () => "anonymous").AllowAnonymous();
        _app.MapGet("/.cratis/identity", () => "identity");
        _app.MapGet("/protected", () => "protected").RequireAuthorization();
        _app.MapPost("/protected", () => "protected").RequireAuthorization();
        _app.MapGet("/socket", async context =>
        {
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
        });
        _app.MapFallback(() => "fallback");
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    protected async Task Send(HttpMethod method, string path, request_headers? headers = null)
    {
        using var request = new HttpRequestMessage(method, path);
        headers?.Apply(request);
        _response = await _client.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
    }

    async Task Destroy()
    {
        _response?.Dispose();
        _client.Dispose();
        await _app.DisposeAsync();
    }
}
