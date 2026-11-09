// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_clearing : given.a_started_host
{
    Task Because() => Switch(name: null);
    [Fact] void should_expire_the_cookie() => Cookie.ShouldContain("expires=");
}
