// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_forwarded_under_a_prefix : given.a_started_host
{
    protected override string PathBase => "/api/play/session";
    protected override string? ForwardedOrigin => "https://stage.example";

    async Task Because()
    {
        await Switch();
        _response.StatusCode.ShouldEqual(HttpStatusCode.NoContent);
        Cookie.ShouldContain("path=/api/play/session");
        Cookie.ShouldContain("secure");
        _cookie = Cookie.Split(';')[0].Split('=')[1];
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_authenticate_same_origin_requests(bool webSocket)
    {
        await SendIdentity(webSocket ? HttpMethod.Get : HttpMethod.Post, origin: "self", webSocket: webSocket);
        _principalId.ShouldEqual("alice");
        _authenticated.ShouldBeTrue();
    }

    [Fact]
    async Task should_preserve_explicit_identity_headers()
    {
        await SendIdentity(HttpMethod.Post, origin: "self", explicitHeader: "explicit-user");
        _principalId.ShouldEqual("explicit-user");
        _principal.ShouldBeNull();
    }

    [Fact]
    async Task should_not_trust_a_rebound_raw_host()
    {
        await SendIdentity(explicitHeader: "spoofed-user", host: "attacker.example");
        _principal.ShouldBeNull();
        _principalId.ShouldBeNull();
        _authenticated.ShouldBeFalse();
    }
}
