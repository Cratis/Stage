// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Components.for_BrowserInvocations;

public class when_the_ability_path_refuses : given.a_browser_invocation_gateway
{
    BrowserInvocationResult _result = null!;

    void Establish() => _invocations.Invoke(Arg.Any<AbilityInvocation>()).Returns(new InvocationAdmission(false, null, "No such ability."));

    async Task Because() => _result = await _gateway.Invoke(Request(), _person);

    [Fact] void should_refuse_it() => _result.Status.ShouldEqual(BrowserInvocationStatus.Refused);
    [Fact] void should_say_why() => _result.Reason.ShouldEqual("No such ability.");
}
