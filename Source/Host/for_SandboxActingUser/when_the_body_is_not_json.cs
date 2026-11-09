// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_the_body_is_not_json : given.a_started_host
{
    Task Because() => Switch(json: false);
    [Fact] void should_refuse_the_write() => _response.StatusCode.ShouldEqual(HttpStatusCode.UnsupportedMediaType);
    [Fact] void should_not_write_a_cookie() => HasCookie.ShouldBeFalse();
}
