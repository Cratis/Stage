// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

[Collection(nameof(offline_authentication_collection))]
public class when_protecting_anonymous_surfaces(OfflineAuthenticationFixture fixture) : Specification
{
    readonly List<HttpStatusCode> _statuses = [];
    readonly List<bool> _redirects = [];
    bool _authenticatedStatic;

    async Task Because()
    {
        using var scenario = fixture.BeginScenario();
        await using var host = await OfflineHost.Start(fixture);
        foreach (var path in new[] { "/api/command", "/api/query", "/.cratis", "/.cratis/discovery", "/healthz", "/swagger", "/index.html", "/customization", "/signin-oidc-extra", "/signin-oidc/nested", "/signout-oidc", "/signout-callback-oidc", "/public.txt" })
        {
            using var response = await host.Send(path);
            _statuses.Add(response.StatusCode);
            _redirects.Add(response.Headers.Location is not null);
        }

        foreach (var path in new[] { "/api/query", "/.cratis/discovery", "/signout-oidc", "/signout-callback-oidc" })
        {
            using var response = await host.Send(path, new HostRequest { Navigate = true });
            _statuses.Add(response.StatusCode);
            _redirects.Add(response.Headers.Location is not null);
        }

        using var authenticated = await host.Send("/public.txt", new HostRequest { Cookie = host.SessionCookie() });
        _authenticatedStatic = authenticated.StatusCode == HttpStatusCode.OK && await authenticated.Content.ReadAsStringAsync() == "offline-static-content";
    }

    [Fact] void should_allow_the_real_static_file_middleware_after_authentication() => _authenticatedStatic.ShouldBeTrue();
    [Fact] void should_require_authentication_even_for_allow_anonymous_endpoints() => _statuses.ShouldEqual(Enumerable.Repeat(HttpStatusCode.Unauthorized, 17));
    [Fact] void should_not_redirect_api_discovery_or_unused_protocol_requests_to_html() => _redirects.ShouldEqual(Enumerable.Repeat(false, 17));
}
