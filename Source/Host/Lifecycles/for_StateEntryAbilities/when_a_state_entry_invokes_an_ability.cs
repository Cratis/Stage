// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Cratis.Stage.Host.Invocations;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Lifecycles.for_StateEntryAbilities;

public class when_a_state_entry_invokes_an_ability : given.a_state_entry_that_invokes_an_ability
{
    readonly List<AbilityInvocation> _requests = [];

    void Establish() => _invocations.Invoke(Arg.Do<AbilityInvocation>(_requests.Add)).Returns(new InvocationAdmission(true, null, string.Empty));

    async Task Because()
    {
        await _abilities.Invoke(_entry, _subject);

        // The driver delivers the same entering again after a restart.
        await _abilities.Invoke(_entry, _subject);
    }

    [Fact] void should_invoke_the_ability_of_the_system() => (_requests[0].System == "Billing" && _requests[0].Ability == "Charge").ShouldBeTrue();
    [Fact] void should_name_the_state_entry_as_the_origin() => _requests[0].Origin.Kind.ShouldEqual(InvocationOriginKind.StateMachineEntry);
    [Fact] void should_address_the_entry_within_the_machine() => _requests[0].Origin.Address.ShouldEqual("InvoiceLifecycle/Sent/entering 1");
    [Fact] void should_tie_the_causation_to_the_instance_and_the_entering() => _requests[0].Origin.Causation.ShouldEqual($"{a_lifecycle_source.InvoiceId}:entered:InvoiceLifecycle:Sent:4");
    [Fact] void should_compose_the_input_from_the_subject() => JsonNode.Parse(_requests[0].Input)!["overdue"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_ask_for_the_same_invocation_when_delivered_again() => _requests[1].Origin.ShouldEqual(_requests[0].Origin);
}
