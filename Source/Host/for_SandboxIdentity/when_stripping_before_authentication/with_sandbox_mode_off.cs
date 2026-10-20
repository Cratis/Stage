// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_stripping_before_authentication;

public class with_sandbox_mode_off : given.a_started_host
{
    protected override bool SandboxEnabled => false;

    Task Because() => Send();

    [Fact] void should_succeed() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_run_authentication() => _recording.Invocations.ShouldNotBeEmpty();
    [Fact] void should_let_authentication_see_the_identity_headers() => _recording.Invocations.Sum(seen => seen.Length).ShouldEqual(4);
    [Fact] void should_keep_the_identity_headers() => _headersSeenByEndpoint.Length.ShouldEqual(4);
}
