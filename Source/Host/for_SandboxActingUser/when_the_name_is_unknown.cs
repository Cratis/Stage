// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_the_name_is_unknown : given.a_started_host
{
    Task Because() => Switch(name: "administrator");
    [Fact] void should_reject_the_selection() => _response.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_not_write_a_cookie() => HasCookie.ShouldBeFalse();
}
