// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_posting_to_the_selection : given.a_started_host
{
    Task Because() => Switch(method: HttpMethod.Post);
    [Fact] void should_not_accept_post() => _response.StatusCode.ShouldEqual(HttpStatusCode.MethodNotAllowed);
    [Fact] void should_not_write_a_cookie() => HasCookie.ShouldBeFalse();
}
