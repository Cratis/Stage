// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_unsafe_requests_carry_a_cookie : given.a_started_host
{
    [Theory]
    [InlineData(null, null, false)]
    [InlineData("https://attacker.example", null, false)]
    [InlineData("null", null, false)]
    [InlineData("not-an-origin", null, false)]
    [InlineData("https://attacker.example", "self", false)]
    [InlineData(null, "https://attacker.example/page", false)]
    [InlineData(null, null, true)]
    [InlineData("https://attacker.example", null, true)]
    async Task should_not_inject_the_identity(string? origin, string? referer, bool webSocket)
    {
        await SendIdentity(webSocket ? HttpMethod.Get : HttpMethod.Post, origin, webSocket: webSocket, referer: referer);
        _principal.ShouldBeNull();
        _authenticated.ShouldBeFalse();
    }
}
