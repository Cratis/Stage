// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_development_is_disabled : given.a_started_host
{
    protected override bool Development => false;
    Task Because() => Switch();
    [Fact] void should_hide_the_surface() => _response.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_write_a_cookie() => HasCookie.ShouldBeFalse();
}
