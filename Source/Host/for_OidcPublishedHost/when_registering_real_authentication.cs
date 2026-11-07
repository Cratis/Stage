// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Cratis.Stage.Host.Published;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Cratis.Stage.Host.for_OidcPublishedHost;

public class when_registering_real_authentication : given.an_oidc_configuration
{
    ServiceProvider _services = null!;
    CookieAuthenticationOptions _cookie = null!;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(_values).Build());
        services.AddDataProtection();
        PublishedAuthentication.Configure(services, Options());
        services.PostConfigure<JwtBearerOptions>(PublishedAuthentication.Bearer, options => options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(new OpenIdConnectConfiguration()));
        _services = services.BuildServiceProvider();
        _cookie = _services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(PublishedAuthentication.Cookie);
    }

    [Fact]
    void should_pin_secure_http_only_lax_cookies()
    {
        _cookie.Cookie.SecurePolicy.ShouldEqual(CookieSecurePolicy.Always);
        _cookie.Cookie.HttpOnly.ShouldBeTrue();
        _cookie.Cookie.SameSite.ShouldEqual(SameSiteMode.Lax);
    }

    [Fact]
    async Task should_read_a_real_cookie_but_never_fall_back_for_invalid_bearer()
    {
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Staff")], PublishedAuthentication.Cookie)), new AuthenticationProperties(), PublishedAuthentication.Cookie);
        await using var cookieScope = _services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = cookieScope.ServiceProvider };
        context.Request.Scheme = "https";
        context.Request.Headers.Cookie = $"{_cookie.Cookie.Name}={_cookie.TicketDataFormat.Protect(ticket)}";
        var cookieResult = await context.AuthenticateAsync(PublishedAuthentication.Selector);
        cookieResult.Succeeded.ShouldBeTrue();
        await using var bearerScope = _services.CreateAsyncScope();
        var bearerContext = new DefaultHttpContext { RequestServices = bearerScope.ServiceProvider };
        bearerContext.Request.Headers.Cookie = context.Request.Headers.Cookie;
        bearerContext.Request.Headers.Authorization = "Bearer invalid";
        var bearerResult = await bearerContext.AuthenticateAsync(PublishedAuthentication.Selector);
        bearerResult.Succeeded.ShouldBeFalse();
    }

    [Fact]
    async Task should_pin_redirects_to_the_configured_origin()
    {
        var options = _services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(PublishedAuthentication.Oidc);
        var context = new DefaultHttpContext { RequestServices = _services };
        context.Request.Host = new HostString("attacker.example");
        var redirect = new RedirectContext(context, new AuthenticationScheme(PublishedAuthentication.Oidc, null, typeof(OpenIdConnectHandler)), options, new AuthenticationProperties())
        {
            ProtocolMessage = new OpenIdConnectMessage()
        };
        await options.Events.OnRedirectToIdentityProvider(redirect);
        redirect.ProtocolMessage.RedirectUri.ShouldEqual("https://app.example/signin-oidc");
        await options.Events.OnRedirectToIdentityProviderForSignOut(redirect);
        redirect.ProtocolMessage.PostLogoutRedirectUri.ShouldEqual("https://app.example/signout-callback-oidc");
    }

    async Task Destroy() => await _services.DisposeAsync();
}
