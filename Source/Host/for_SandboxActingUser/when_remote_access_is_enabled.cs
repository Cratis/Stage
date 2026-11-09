// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_remote_access_is_enabled : given.a_started_host
{
    protected override bool AllowRemote => true;

    [Fact]
    async Task should_allow_selection_for_the_remote_host()
    {
        await Switch(origin: "http://sandbox.example", host: "sandbox.example");
        _response.StatusCode.ShouldEqual(HttpStatusCode.NoContent);
        Cookie.ShouldContain("stage-acting-user=alice;");
    }

    [Fact]
    async Task should_still_reject_a_cross_origin_switch()
    {
        await Switch(origin: "http://attacker.example", host: "sandbox.example");
        _response.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
        HasCookie.ShouldBeFalse();
    }

    [Fact]
    async Task should_apply_the_cookie_for_the_remote_host()
    {
        await SendIdentity(host: "sandbox.example");
        _principalId.ShouldEqual("alice");
        _authenticated.ShouldBeTrue();
    }
}
