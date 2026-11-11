// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_a_safe_ability_fails_and_is_retried : an_invocation_silo
{
    InvocationRecord _settled = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Look", EffectClass.Safe)];

    async Task Because()
    {
        _backend.Behavior = call => Task.FromResult<AbilityResult>(call.Attempt == 1 ? new AbilityFailed("connection reset") : new AbilityReturned());
        await Invocations.Invoke(new("Slack", "Look", Origin("event 3")));
        await Eventually(() => !_reactions.Settlements.IsEmpty, "the reaction branch to settle");
        _settled = _reactions.Settlements.Single();
    }

    [Fact] void should_call_the_extension_again() => _backend.Calls.Count.ShouldEqual(2);
    [Fact] void should_number_the_attempts() => _backend.Calls.Select(call => call.Attempt).Order().SequenceEqual([1, 2]).ShouldBeTrue();
    [Fact] void should_give_every_attempt_the_same_idempotency_key() => _backend.Calls.Select(call => call.IdempotencyKey).Distinct().Count().ShouldEqual(1);
    [Fact] void should_settle_with_the_outcome_of_the_last_attempt() => InvocationBranch.Of(_settled).ShouldEqual("returned");
}
