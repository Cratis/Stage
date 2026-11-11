// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Components.for_BrowserInvocations;

public class when_the_person_is_not_authenticated : given.a_browser_invocation_gateway
{
    BrowserInvocationResult _result = null!;

    async Task Because() => _result = await _gateway.Invoke(Request(), new ClaimsPrincipal(new ClaimsIdentity()));

    [Fact] void should_refuse_it_as_forbidden() => _result.Status.ShouldEqual(BrowserInvocationStatus.Forbidden);
    [Fact] void should_not_reach_the_ability_path() => _invocations.ReceivedCalls().ShouldBeEmpty();
}
