// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_the_host_is_rebound : given.a_started_host
{
    async Task Because()
    {
        await SendIdentity(host: "attacker.example");
        await Switch(origin: "http://attacker.example", host: "attacker.example");
    }
    [Fact] void should_hide_the_surface() => _response.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_write_a_cookie() => HasCookie.ShouldBeFalse();

    [Fact] void should_not_inject_a_principal() => _principal.ShouldBeNull();
    [Fact] void should_not_inject_a_principal_id() => _principalId.ShouldBeNull();
    [Fact] void should_not_authenticate_the_cookie() => _authenticated.ShouldBeFalse();
}
