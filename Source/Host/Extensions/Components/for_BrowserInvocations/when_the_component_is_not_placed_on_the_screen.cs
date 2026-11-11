// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Components.for_BrowserInvocations;

public class when_the_component_is_not_placed_on_the_screen : given.a_browser_invocation_gateway
{
    BrowserInvocationResult _result = null!;

    async Task Because() => _result = await _gateway.Invoke(Request() with { Screen = "Reports" }, _person);

    [Fact] void should_refuse_it_as_not_placed() => _result.Status.ShouldEqual(BrowserInvocationStatus.NotPlaced);
    [Fact] void should_not_reach_the_ability_path() => _invocations.ReceivedCalls().ShouldBeEmpty();
}
