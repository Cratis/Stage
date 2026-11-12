// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Components.for_BrowserInvocations;

public class when_invoking_an_ability_outside_the_allowlist : given.a_browser_invocation_gateway
{
    BrowserInvocationResult _result = null!;

    async Task Because() => _result = await _gateway.Invoke(Request("DeleteChannel"), _person);

    [Fact] void should_refuse_it_as_not_allowed() => _result.Status.ShouldEqual(BrowserInvocationStatus.NotAllowed);
    [Fact] void should_not_reach_the_ability_path() => _invocations.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_check_authorization_first() => _authorization.ReceivedCalls().ShouldBeEmpty();
}
