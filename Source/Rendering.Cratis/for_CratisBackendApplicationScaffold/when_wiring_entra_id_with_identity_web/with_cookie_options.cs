// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_wiring_entra_id_with_identity_web.given;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_wiring_entra_id_with_identity_web;

public class with_cookie_options : a_configured_authentication_host
{
    CookieAuthenticationOptions _options = null!;
    DefaultHttpContext _login = null!;
    DefaultHttpContext _denied = null!;

    async Task Because()
    {
        _options = _app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        _login = new DefaultHttpContext();
        _denied = new DefaultHttpContext();
        await _options.Events.OnRedirectToLogin(Redirect(_login));
        await _options.Events.OnRedirectToAccessDenied(Redirect(_denied));
    }

    [Fact] void should_keep_the_session_cookie_http_only() => _options.Cookie.HttpOnly.ShouldBeTrue();
    [Fact] void should_always_secure_the_session_cookie() => _options.Cookie.SecurePolicy.ShouldEqual(CookieSecurePolicy.Always);
    [Fact] void should_use_lax_same_site_for_the_session_cookie() => _options.Cookie.SameSite.ShouldEqual(SameSiteMode.Lax);
    [Fact] void should_return_unauthorized_instead_of_redirecting_to_login() => _login.Response.StatusCode.ShouldEqual(StatusCodes.Status401Unauthorized);
    [Fact] void should_not_set_a_login_location() => _login.Response.Headers.ContainsKey("Location").ShouldBeFalse();
    [Fact] void should_return_forbidden_instead_of_redirecting_to_access_denied() => _denied.Response.StatusCode.ShouldEqual(StatusCodes.Status403Forbidden);
    [Fact] void should_not_set_an_access_denied_location() => _denied.Response.Headers.ContainsKey("Location").ShouldBeFalse();

    RedirectContext<CookieAuthenticationOptions> Redirect(HttpContext context) => new(
        context,
        new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler)),
        _options,
        new AuthenticationProperties(),
        "https://untrusted.invalid/account");
}
