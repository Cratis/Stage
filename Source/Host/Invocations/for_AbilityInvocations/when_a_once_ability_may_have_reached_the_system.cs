// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_a_once_ability_may_have_reached_the_system : an_invocation_silo
{
    InvocationRecord _stopped = null!;
    IReadOnlyList<InvocationRecord> _listed = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Pay", EffectClass.Once)];

    async Task Because()
    {
        _backend.Behavior = _ => throw new TimeoutException("no answer from the payment provider");
        await Invocations.Invoke(new("Slack", "Pay", Origin("event 12")));
        await Eventually(async () => (await Invocations.List(InvocationStatus.Stopped)).Count == 1, "the invocation to stop");

        // Long enough for the retry delay and every attempt to have happened if anything was going to be retried.
        await Task.Delay(1500);
        _listed = await Invocations.List(InvocationStatus.Stopped);
        _stopped = _listed.Single();
    }

    [Fact] void should_call_the_extension_only_once() => _backend.Calls.Count.ShouldEqual(1);
    [Fact] void should_stop_for_reconciliation() => _stopped.Status.ShouldEqual(InvocationStatus.Stopped);
    [Fact] void should_say_what_is_unknown() => _stopped.Reason.Contains("no answer from the payment provider", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_settle_the_origin() => _reactions.Settlements.IsEmpty.ShouldBeTrue();
    [Fact] void should_keep_the_attempt_that_may_have_reached_the_system() => _stopped.AttemptInFlight.ShouldEqual(1);
    [Fact] void should_list_the_invocation_for_the_workbench_with_its_run_and_cause() => (_stopped.Run.Length > 0 && _stopped.Origin.Causation == "event 12").ShouldBeTrue();
}
