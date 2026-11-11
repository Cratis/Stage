// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_the_cause_is_delivered_again : an_invocation_silo
{
    InvocationAdmission _first = null!;
    InvocationAdmission _again = null!;
    InvocationAdmission _afterSettling = null!;
    InvocationAdmission _anotherCause = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Charge", EffectClass.Once)];

    async Task Because()
    {
        _first = await Invocations.Invoke(new("Slack", "Charge", Origin("event 31")));
        _again = await Invocations.Invoke(new("Slack", "Charge", Origin("event 31")));
        await Eventually(() => !_reactions.Settlements.IsEmpty, "the reaction branch to settle");
        _afterSettling = await Invocations.Invoke(new("Slack", "Charge", Origin("event 31")));
        _anotherCause = await Invocations.Invoke(new("Slack", "Charge", Origin("event 32")));
        await Eventually(() => _reactions.Settlements.Count == 2, "the second cause to settle");
        await Task.Delay(500);
    }

    [Fact] void should_find_the_invocation_that_exists() => _again.Record!.Identity.ShouldEqual(_first.Record!.Identity);
    [Fact] void should_find_it_after_it_settled() => _afterSettling.Record!.Identity.ShouldEqual(_first.Record!.Identity);
    [Fact] void should_find_the_run_that_exists() => (_first.Record!.Run.Length > 0 && _again.Record!.Run == _first.Record.Run).ShouldBeTrue();
    [Fact] void should_call_the_extension_once_for_each_cause() => _backend.Calls.Count.ShouldEqual(2);
    [Fact] void should_give_another_cause_another_identity() => _anotherCause.Record!.Identity.ShouldNotEqual(_first.Record!.Identity);
    [Fact] void should_settle_the_origin_once_for_each_cause() => _reactions.Settlements.Count.ShouldEqual(2);
}
