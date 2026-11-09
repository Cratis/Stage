// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_headers_are_explicit : given.a_started_host
{
    Task Because() => SendIdentity(explicitHeader: "lens-user");
    [Fact] void should_preserve_the_explicit_selection() => _principalId.ShouldEqual("lens-user");
    [Fact] void should_not_merge_the_cookie_principal() => _principal.ShouldBeNull();
}
