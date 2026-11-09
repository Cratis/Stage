// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_fetch_metadata_disagrees : given.a_started_host
{
    Task Because() => Switch(site: "cross-site");
    [Fact] void should_refuse_the_write() => _response.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_not_write_a_cookie() => HasCookie.ShouldBeFalse();
}
