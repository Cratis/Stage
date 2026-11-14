// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

[Collection(nameof(offline_authentication_collection))]
public class when_redeeming_an_offline_browser_code(OfflineAuthenticationFixture fixture) : Specification
{
    readonly OfflineBrowser _browser = new();
    HttpStatusCode _authenticated;
    bool _identity;
    bool _retainedBindings;
    int _redemptions;
    OfflineAuthenticationSnapshot _snapshot = null!;

    async Task Because()
    {
        using var scenario = fixture.BeginScenario();
        await using (var previous = await OfflineHost.Start(fixture))
        {
            using var response = await previous.Send("/api/query", new HostRequest { Authorization = $"Bearer {previous.Issuer.Token()}" });
            (response.StatusCode == HttpStatusCode.OK).ShouldBeTrue();
            fixture.AssertBindings(previous.Services);
        }

        // The first host warms the process-wide issuer cache. Its retained client and signer must survive.
        await fixture.AssertRetainedClients();
        await using var host = await OfflineHost.Start(fixture);
        fixture.AssertBindings(host.Services);
        _retainedBindings = true;
        await _browser.Login(host);
        _redemptions = host.Issuer.Redemptions;
        using var authenticated = await host.Send("/api/query", new HostRequest { Cookie = _browser.Cookie });
        _authenticated = authenticated.StatusCode;
        _identity = await authenticated.Content.ReadAsStringAsync() == "authenticated";
        _snapshot = scenario.Diagnostics.Snapshot();
    }

    [Fact]
    void should_complete_the_real_oidc_callback_and_code_redemption()
    {
        Console.WriteLine(_snapshot);
        (_retainedBindings && _browser.Status == HttpStatusCode.Redirect && _redemptions == 1).ShouldBeTrue();
        (_browser.SecureSession && _authenticated == HttpStatusCode.OK && _identity).ShouldBeTrue();
        (_snapshot.Token == 1 && _snapshot.Unexpected == 0 && _snapshot.Unsupported == 0 && _snapshot.Failures == 0).ShouldBeTrue();
    }

    [Fact] void should_issue_a_secure_http_only_same_site_lax_session() => _browser.SecureSession.ShouldBeTrue();
    [Fact] void should_authenticate_a_following_request_with_the_issued_cookie() => (_authenticated == HttpStatusCode.OK && _identity).ShouldBeTrue();
}
