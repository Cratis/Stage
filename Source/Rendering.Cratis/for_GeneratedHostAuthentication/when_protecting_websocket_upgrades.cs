// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

[Collection(nameof(offline_authentication_collection))]
public class when_protecting_websocket_upgrades(OfflineAuthenticationFixture fixture) : Specification
{
    HttpStatusCode _accepted;
    HttpStatusCode _bearer;
    HttpStatusCode _anonymous;
    readonly List<HttpStatusCode> _rejected = [];

    async Task Because()
    {
        using var scenario = fixture.BeginScenario();
        await using var host = await OfflineHost.Start(fixture);
        var cookie = host.SessionCookie();
        foreach (var origin in new[] { null, "null", "https://foreign.example.test" })
        {
            _rejected.Add(await host.Upgrade(new HostRequest { Origin = origin, Cookie = cookie }));
        }

        _accepted = await host.Upgrade(new HostRequest { Origin = OfflineIssuer.Origin, Cookie = cookie });
        _bearer = await host.Upgrade(new HostRequest { Authorization = $"Bearer {host.Issuer.Token()}" });
        _anonymous = await host.Upgrade(new HostRequest { Origin = OfflineIssuer.Origin });
    }

    [Fact] void should_establish_a_real_same_origin_upgrade_after_cookie_authentication() => _accepted.ShouldEqual(HttpStatusCode.SwitchingProtocols);
    [Fact] void should_allow_bearer_upgrades_without_origin() => _bearer.ShouldEqual(HttpStatusCode.SwitchingProtocols);
    [Fact] void should_return_401_for_an_anonymous_upgrade_without_redirecting() => _anonymous.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_reject_missing_null_and_foreign_origin_before_dispatch() => _rejected.ShouldEqual(Enumerable.Repeat(HttpStatusCode.Forbidden, 3));
}
