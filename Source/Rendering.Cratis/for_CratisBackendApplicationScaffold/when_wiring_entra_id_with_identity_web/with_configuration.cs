// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_wiring_entra_id_with_identity_web.given;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_wiring_entra_id_with_identity_web;

public class with_configuration : a_configured_authentication_host
{
    string[] _schemes = [];
    AuthenticationOptions _defaults = null!;
    PolicySchemeOptions _selector = null!;

    async Task Because()
    {
        var provider = _app.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        _schemes = [.. (await provider.GetAllSchemesAsync()).Select(scheme => scheme.Name)];
        _defaults = _app.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        _selector = _app.Services.GetRequiredService<IOptionsMonitor<PolicySchemeOptions>>().Get(Selector);
    }

    [Fact] void should_register_exactly_the_selector_cookie_openid_connect_and_bearer_schemes() => _schemes.Order().ShouldEqual(new[] { Selector, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme }.Order());
    [Fact] void should_default_to_the_selector() => _defaults.DefaultScheme.ShouldEqual(Selector);
    [Fact] void should_authenticate_through_the_selector() => _defaults.DefaultAuthenticateScheme.ShouldEqual(Selector);
    [Fact] void should_challenge_through_the_selector() => _defaults.DefaultChallengeScheme.ShouldEqual(Selector);
    [Fact] void should_forbid_through_the_selector() => _defaults.DefaultForbidScheme.ShouldEqual(Selector);
    [Fact] void should_configure_the_forwarding_selector() => _selector.ForwardDefaultSelector.ShouldNotBeNull();
    [Fact] void should_register_token_acquisition_without_resolving_it() => _builder.Services.Any(descriptor => descriptor.ServiceType == typeof(ITokenAcquisition)).ShouldBeTrue();
}
