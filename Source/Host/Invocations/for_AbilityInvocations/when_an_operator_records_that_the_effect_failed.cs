// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_an_operator_records_that_the_effect_failed : an_invocation_silo
{
    InvocationRecord _settled = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Pay", EffectClass.Once)];

    async Task Because()
    {
        _backend.Behavior = _ => throw new TimeoutException("no answer");
        var admission = await Invocations.Invoke(new("Slack", "Pay", Origin("event 22")));
        await Eventually(async () => (await Invocations.List(InvocationStatus.Stopped)).Count == 1, "the invocation to stop");
        await Invocations.Reconcile(admission.Record!.Identity, "failed", string.Empty, "alice");
        await Eventually(() => !_reactions.Settlements.IsEmpty, "the reaction branch to settle");
        _settled = _reactions.Settlements.Single();
    }

    [Fact] void should_settle_the_origin_with_the_failed_branch() => InvocationBranch.Of(_settled).ShouldEqual("failed");
    [Fact] void should_end_the_invocation_as_failed() => _settled.Status.ShouldEqual(InvocationStatus.Failed);
    [Fact] void should_record_who_chose() => _settled.DecidedBy.ShouldEqual("alice");
    [Fact] void should_not_call_the_extension_again() => _backend.Calls.Count.ShouldEqual(1);
}
