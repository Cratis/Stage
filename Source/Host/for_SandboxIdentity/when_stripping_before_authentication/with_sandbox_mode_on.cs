// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_stripping_before_authentication;

public class with_sandbox_mode_on : given.a_started_host
{
    Task Because() => Send();

    [Fact] void should_succeed() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_run_authentication() => _recording.Invocations.ShouldNotBeEmpty();
    [Fact] void should_not_let_authentication_see_any_identity_header() => _recording.Invocations.SelectMany(seen => seen).ShouldBeEmpty();
    [Fact] void should_not_forward_any_identity_header() => _headersSeenByEndpoint.ShouldBeEmpty();
}
