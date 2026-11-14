// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

[Collection(nameof(offline_authentication_collection))]
public class when_challenging_browser_navigation(OfflineAuthenticationFixture fixture) : Specification
{
    HttpStatusCode _status;
    bool _pinnedCallback;
    bool _codeWithPkce;
    bool _stateAndNonce;
    bool _secureProtocolCookies;
    bool _boundedSession;
    bool _cookieSignIn;

    async Task Because()
    {
        using var scenario = fixture.BeginScenario();
        await using var host = await OfflineHost.Start(fixture);
        using var response = await host.Send("/", new HostRequest { Navigate = true });
        _status = response.StatusCode;
        var query = QueryHelpers.ParseQuery(response.Headers.Location!.Query);
        _pinnedCallback = query["redirect_uri"] == $"{OfflineIssuer.Origin}/signin-oidc";
        _codeWithPkce = query["response_type"] == "code" && query["response_mode"] == "form_post" && query["code_challenge_method"] == "S256" && !string.IsNullOrEmpty(query["code_challenge"]);
        _stateAndNonce = !string.IsNullOrEmpty(query["state"]) && !string.IsNullOrEmpty(query["nonce"]);
        _secureProtocolCookies = HasSecureProtocolCookies(response);
        var cookieOptions = host.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(OfflineHost.CookieScheme);
        _boundedSession = cookieOptions.ExpireTimeSpan == TimeSpan.FromHours(8) && !cookieOptions.SlidingExpiration;
        _cookieSignIn = host.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OfflineHost.OidcScheme).SignInScheme == OfflineHost.CookieScheme;
    }

    static bool HasSecureProtocolCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Count() == 2 && cookies.All(cookie =>
            cookie.Contains("secure", StringComparison.OrdinalIgnoreCase) && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && cookie.Contains("samesite=none", StringComparison.OrdinalIgnoreCase));

    [Fact] void should_redirect_only_the_browser_document_navigation() => _status.ShouldEqual(HttpStatusCode.Redirect);
    [Fact] void should_ignore_host_and_forwarded_headers_when_building_the_callback() => _pinnedCallback.ShouldBeTrue();
    [Fact] void should_request_code_redemption_with_pkce() => _codeWithPkce.ShouldBeTrue();
    [Fact] void should_generate_state_nonce_and_secure_correlation_cookies() => (_stateAndNonce && _secureProtocolCookies).ShouldBeTrue();
    [Fact] void should_sign_in_to_a_bounded_non_sliding_cookie_session() => (_boundedSession && _cookieSignIn).ShouldBeTrue();
}
