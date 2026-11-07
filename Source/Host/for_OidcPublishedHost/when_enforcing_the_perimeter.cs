// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Cratis.Specifications;
using Cratis.Stage.Host.Published;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.for_OidcPublishedHost;

public class when_enforcing_the_perimeter : given.an_oidc_configuration
{
    ServiceProvider _services = null!;
    RequestDelegate _pipeline = null!;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRouting();
        services.AddAuthorization();
        services.AddAuthentication(PublishedAuthentication.Selector)
            .AddPolicyScheme(PublishedAuthentication.Selector, null, options => options.ForwardDefaultSelector = PublishedAuthentication.SelectScheme)
            .AddScheme<AuthenticationSchemeOptions, StubAuthentication>(PublishedAuthentication.Cookie, _ => { })
            .AddScheme<AuthenticationSchemeOptions, StubAuthentication>(PublishedAuthentication.Bearer, _ => { })
            .AddScheme<AuthenticationSchemeOptions, StubAuthentication>(PublishedAuthentication.Oidc, _ => { });
        _services = services.BuildServiceProvider();
        var app = new ApplicationBuilder(_services);
        PublishedAuthentication.Use(app, Options());
        app.Run(context =>
        {
            context.Response.StatusCode = context.Request.Headers.Keys.Any(name => name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase) || name.StartsWith("x-stage-persona", StringComparison.OrdinalIgnoreCase)) ? 500 : 200;
            return Task.CompletedTask;
        });
        _pipeline = app.Build();
    }

    [Theory]
    [InlineData("GET", "/", false, false, "text/html", null, false, 302)]
    [InlineData("GET", "/stage/status", false, false, null, null, false, 401)]
    [InlineData("GET", "/app.js", false, false, null, null, false, 401)]
    [InlineData("GET", "/", true, true, "text/html", null, false, 401)]
    [InlineData("GET", "/", true, false, null, null, false, 200)]
    [InlineData("POST", "/api/command", true, false, null, "https://foreign.example", false, 403)]
    [InlineData("POST", "/api/command", true, false, null, null, false, 403)]
    [InlineData("POST", "/api/command", true, false, null, "https://app.example", false, 200)]
    [InlineData("POST", "/api/command", true, false, null, "http://app.example", false, 403)]
    [InlineData("POST", "/api/command", true, false, null, "https://app.example:444", false, 403)]
    [InlineData("GET", "/socket", true, false, null, "https://foreign.example", true, 403)]
    [InlineData("GET", "/socket", true, false, null, "https://app.example", true, 200)]
    [InlineData("GET", "/healthz", false, false, null, null, false, 200)]
    [InlineData("HEAD", "/healthz", false, false, null, null, false, 200)]
    [InlineData("POST", "/healthz", false, false, null, "https://app.example", false, 401)]
    async Task should_gate_every_request(string method, string path, bool cookie, bool authorization, string? accept, string? origin, bool upgrade, int status)
    {
        var context = new DefaultHttpContext { RequestServices = _services };
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Host = new HostString("attacker.example");
        context.Request.Headers["x-ms-client-principal"] = "untrusted";
        context.Request.Headers["x-ms-client-principal-id"] = "untrusted";
        context.Request.Headers["X-Stage-Persona"] = "Staff";
        if (cookie) context.Request.Headers["X-Spec-Cookie"] = "yes";
        if (authorization) context.Request.Headers.Authorization = "Bearer invalid";
        if (accept is not null) context.Request.Headers.Accept = accept;
        if (origin is not null) context.Request.Headers.Origin = origin;
        if (upgrade) context.Request.Headers.Upgrade = "websocket";
        await _pipeline(context);
        context.Response.StatusCode.ShouldEqual(status);
        if (status == 302) context.Response.Headers.Location.ToString().ShouldEqual("/signin?return=/");
    }

    async Task Destroy() => await _services.DisposeAsync();

    sealed class StubAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
            Scheme.Name == PublishedAuthentication.Cookie && Request.Headers.ContainsKey("X-Spec-Cookie")
                ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Staff")], Scheme.Name)), Scheme.Name))
                : AuthenticateResult.NoResult());

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            if (Scheme.Name == PublishedAuthentication.Oidc) Response.Redirect($"/signin?return={properties.RedirectUri}");
            else Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
    }
}
