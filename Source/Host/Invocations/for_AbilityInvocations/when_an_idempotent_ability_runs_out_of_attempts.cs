// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_an_idempotent_ability_runs_out_of_attempts : an_invocation_silo
{
    InvocationRecord _settled = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Charge", EffectClass.Idempotent) with { MaxAttempts = 2 }];

    async Task Because()
    {
        _backend.Behavior = _ => throw new InvalidOperationException("the system is down");
        await Invocations.Invoke(new("Slack", "Charge", Origin("event 9")));
        await Eventually(() => !_reactions.Settlements.IsEmpty, "the reaction branch to settle");
        _settled = _reactions.Settlements.Single();
    }

    [Fact] void should_use_every_attempt_and_no_more() => _backend.Calls.Count.ShouldEqual(2);
    [Fact] void should_retry_with_the_same_idempotency_key() => _backend.Calls.Select(call => call.IdempotencyKey).Distinct().Count().ShouldEqual(1);
    [Fact] void should_settle_the_origin_with_the_failed_branch() => InvocationBranch.Of(_settled).ShouldEqual("failed");
    [Fact] void should_end_the_invocation_as_failed() => _settled.Status.ShouldEqual(InvocationStatus.Failed);
    [Fact] void should_say_why() => _settled.Reason.Contains("the system is down", StringComparison.Ordinal).ShouldBeTrue();
}
