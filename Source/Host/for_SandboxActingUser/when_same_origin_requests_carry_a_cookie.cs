// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_same_origin_requests_carry_a_cookie : given.a_started_host
{
    [Theory]
    [InlineData("self", null, false)]
    [InlineData(null, "self", false)]
    [InlineData("self", null, true)]
    async Task should_authenticate_the_modeled_user(string? origin, string? referer, bool webSocket)
    {
        await SendIdentity(webSocket ? HttpMethod.Get : HttpMethod.Post, origin, webSocket: webSocket, referer: referer);
        _principalId.ShouldEqual("alice");
        _authenticated.ShouldBeTrue();
    }
}
