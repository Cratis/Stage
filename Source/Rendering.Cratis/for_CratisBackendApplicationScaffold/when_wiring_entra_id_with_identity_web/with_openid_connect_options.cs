// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_wiring_entra_id_with_identity_web.given;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_wiring_entra_id_with_identity_web;

public class with_openid_connect_options : a_configured_authentication_host
{
    OpenIdConnectOptions _options = null!;
    RedirectContext _signIn = null!;
    RedirectContext _signOut = null!;

    async Task Because()
    {
        _options = OpenIdConnectOptions();
        _signIn = Redirect();
        _signOut = Redirect();
        await _options.Events.OnRedirectToIdentityProvider(_signIn);
        await _options.Events.OnRedirectToIdentityProviderForSignOut(_signOut);
    }

    [Fact] void should_always_secure_the_correlation_cookie() => _options.CorrelationCookie.SecurePolicy.ShouldEqual(CookieSecurePolicy.Always);
    [Fact] void should_always_secure_the_nonce_cookie() => _options.NonceCookie.SecurePolicy.ShouldEqual(CookieSecurePolicy.Always);
    [Fact] void should_pin_the_sign_in_redirect_to_the_public_origin_and_callback_path() => _signIn.ProtocolMessage.RedirectUri.ShouldEqual(PublicOrigin + _options.CallbackPath);
    [Fact] void should_pin_the_sign_out_redirect_to_the_public_origin_and_signed_out_callback_path() => _signOut.ProtocolMessage.PostLogoutRedirectUri.ShouldEqual(PublicOrigin + _options.SignedOutCallbackPath);

    RedirectContext Redirect()
    {
        var context = new DefaultHttpContext { RequestServices = _app.Services };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("untrusted.invalid");
        return new RedirectContext(
            context,
            new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler)),
            _options,
            new AuthenticationProperties())
        {
            ProtocolMessage = new OpenIdConnectMessage
            {
                RedirectUri = "https://untrusted.invalid/signin-oidc",
                PostLogoutRedirectUri = "https://untrusted.invalid/signout-callback-oidc"
            }
        };
    }
}
