// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

[Collection(nameof(offline_authentication_collection))]
public class when_enforcing_cookie_origins(OfflineAuthenticationFixture fixture) : Specification
{
    readonly List<HttpStatusCode> _accepted = [];
    readonly List<HttpStatusCode> _rejected = [];

    async Task Because()
    {
        using var scenario = fixture.BeginScenario();
        await using var host = await OfflineHost.Start(fixture);
        var cookie = host.SessionCookie();
        foreach (var method in new[] { "POST", "PUT", "PATCH", "DELETE" })
        {
            foreach (var origin in new[] { OfflineIssuer.Origin, "https://APP.example.test:443" })
            {
                using var response = await host.Send("/api/command", new HostRequest { Method = method, Cookie = cookie, Origin = origin });
                _accepted.Add(response.StatusCode);
            }

            foreach (var origin in new[] { null, "null", "https://foreign.example.test", "https://app.example.test:444", "http://app.example.test", "https://app.example.test/path", "https://app.example.test,https://foreign.example.test", "not-an-origin" })
            {
                using var response = await host.Send("/api/command", new HostRequest { Method = method, Cookie = cookie, Origin = origin });
                _rejected.Add(response.StatusCode);
            }
        }

        foreach (var method in new[] { "GET", "HEAD", "OPTIONS" })
        {
            using var response = await host.Send("/api/query", new HostRequest { Method = method, Cookie = cookie });
            _accepted.Add(response.StatusCode);
        }
    }

    [Fact] void should_accept_only_same_origin_cookie_writes_and_safe_requests() => _accepted.ShouldEqual(Enumerable.Repeat(HttpStatusCode.OK, 11));
    [Fact] void should_reject_absent_null_foreign_or_malformed_origins_on_every_unsafe_method() => _rejected.ShouldEqual(Enumerable.Repeat(HttpStatusCode.Forbidden, 32));
}
