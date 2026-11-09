// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_selecting : given.a_started_host
{
    Task Because() => Switch();
    [Fact] void should_accept_the_modeled_name() => _response.StatusCode.ShouldEqual(HttpStatusCode.NoContent);
    [Fact] void should_store_only_the_name() => Cookie.ShouldContain("stage-acting-user=alice;");
    [Fact] void should_be_http_only() => Cookie.ShouldContain("httponly");
    [Fact] void should_use_strict_same_site() => Cookie.ShouldContain("samesite=strict");
    [Fact] void should_be_host_only() => Cookie.ShouldNotContain("domain=");
    [Fact] void should_be_a_session_cookie() => Cookie.ShouldNotContain("expires=");
}
