// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Components.for_BrowserInvocations;

public class when_the_invocation_does_not_settle_in_time : given.a_browser_invocation_gateway
{
    BrowserInvocationResult _result = null!;

    void Establish() => _ledger.Get("invocation-1").Returns(Record(Invocations.InvocationStatus.Running));

    async Task Because() => _result = await _gateway.Invoke(Request(), _person);

    [Fact] void should_answer_that_it_is_pending() => _result.Status.ShouldEqual(BrowserInvocationStatus.Pending);
    [Fact] void should_name_the_invocation_so_it_can_be_asked_for_again() => _result.Identity.ShouldEqual("invocation-1");
}
