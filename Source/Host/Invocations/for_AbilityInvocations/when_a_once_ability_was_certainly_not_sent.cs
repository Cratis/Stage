// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_a_once_ability_was_certainly_not_sent : an_invocation_silo
{
    InvocationRecord _settled = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Pay", EffectClass.Once)];

    async Task Because()
    {
        _backend.Behavior = call => Task.FromResult<AbilityResult>(call.Attempt == 1 ? new AbilityNotSent("no extension is connected yet") : new AbilityReturned());
        await Invocations.Invoke(new("Slack", "Pay", Origin("event 13")));
        await Eventually(() => !_reactions.Settlements.IsEmpty, "the reaction branch to settle");
        _settled = _reactions.Settlements.Single();
    }

    [Fact] void should_try_again_because_nothing_could_have_happened() => _backend.Calls.Count.ShouldEqual(2);
    [Fact] void should_settle_the_origin() => InvocationBranch.Of(_settled).ShouldEqual("returned");
}
