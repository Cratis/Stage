// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_a_prefixed_cookie_is_stale : given.a_started_host
{
    protected override string PathBase => "/api/play/session";
    protected override string? ForwardedOrigin => "https://stage.example";

    void Establish() => _cookie = "removed-user";
    Task Because() => SendIdentity();

    [Fact] void should_not_inject_an_identity() => _principal.ShouldBeNull();
    [Fact] void should_expire_the_selection() => Cookie.ShouldContain("expires=");
    [Fact] void should_clear_the_cookie_at_the_forwarded_path() => Cookie.ShouldContain("path=/api/play/session");
    [Fact] void should_clear_a_secure_cookie() => Cookie.ShouldContain("secure");
}
