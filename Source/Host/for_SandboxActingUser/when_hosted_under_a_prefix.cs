// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_hosted_under_a_prefix : given.a_started_host
{
    protected override string PathBase => "/api/play/session";
    Task Because() => Switch();
    [Fact] void should_scope_the_cookie_to_the_session() => Cookie.ShouldContain("path=/api/play/session");
}
