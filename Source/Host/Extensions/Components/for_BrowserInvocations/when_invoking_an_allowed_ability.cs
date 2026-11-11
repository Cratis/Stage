// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Components.for_BrowserInvocations;

public class when_invoking_an_allowed_ability : given.a_browser_invocation_gateway
{
    BrowserInvocationResult _result = null!;

    async Task Because() => _result = await _gateway.Invoke(Request(), _person);

    [Fact] void should_settle() => _result.Status.ShouldEqual(BrowserInvocationStatus.Settled);
    [Fact] void should_return_the_outcome() => _result.Outcome.ShouldEqual("returned");
    [Fact] void should_return_what_the_ability_returned() => _result.Output.ShouldEqual("""{"ts":"1"}""");
    [Fact] void should_name_the_invocation() => _result.Identity.ShouldEqual("invocation-1");
    [Fact] void should_invoke_the_ability_of_the_system() => (_invoked!.System, _invoked.Ability).ShouldEqual(("Slack", "Send"));
    [Fact] void should_pass_the_input_on() => _invoked!.Input.ShouldEqual("""{"channel":"ops"}""");
    [Fact] void should_come_from_a_screen_interaction() => _invoked!.Origin.Kind.ShouldEqual(InvocationOriginKind.ScreenInteraction);
    [Fact] void should_address_the_component_on_its_screen() => _invoked!.Origin.Address.ShouldEqual("Orders/Slack.ChannelPicker");
    [Fact] void should_tie_the_interaction_to_the_person() => _invoked!.Origin.Causation.ShouldEqual("ada/click-1");
}
