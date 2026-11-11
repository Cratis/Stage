// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_a_safe_ability_returns : an_invocation_silo
{
    InvocationAdmission _admission = null!;
    InvocationRecord _settled = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Post", EffectClass.Safe)];

    async Task Because()
    {
        _backend.Behavior = _ => Task.FromResult<AbilityResult>(new AbilityReturned(null, """{"ts":"1.2"}"""));
        _admission = await Invocations.Invoke(new("Slack", "Post", Origin("event 41"), """{"text":"hi"}"""));
        await Eventually(() => !_reactions.Settlements.IsEmpty, "the reaction branch to settle");
        _settled = _reactions.Settlements.Single();
    }

    [Fact] void should_admit_the_invocation() => _admission.Admitted.ShouldBeTrue();
    [Fact] void should_call_the_extension_once() => _backend.Calls.Count.ShouldEqual(1);
    [Fact] void should_give_the_extension_the_invocation_identity_as_the_idempotency_key() => _backend.Calls.Single().IdempotencyKey.ShouldEqual(_admission.Record!.Identity);
    [Fact] void should_give_the_extension_the_input_and_the_pinned_package_digest() => (_backend.Calls.Single().Input.Contains("hi", StringComparison.Ordinal) && _backend.Calls.Single().PackageDigest == "sha256:aaaa").ShouldBeTrue();
    [Fact] void should_run_under_the_capability_with_the_contract_major() => _settled.Capability.ShouldEqual("ability:Slack.Post@1");
    [Fact] void should_settle_the_origin_with_the_returned_branch() => InvocationBranch.Of(_settled).ShouldEqual("returned");
    [Fact] void should_carry_what_the_ability_returned() => _settled.Output.ShouldEqual("""{"ts":"1.2"}""");
    [Fact] void should_record_the_origin_and_its_cause() => (_settled.Origin.Address == "Fulfillment/PostToWarehouse/branch 1" && _settled.Origin.Causation == "event 41").ShouldBeTrue();
    [Fact] void should_end_the_invocation_as_settled() => _settled.Status.ShouldEqual(InvocationStatus.Settled);
    [Fact] void should_not_settle_any_other_origin() => (_stateMachines.Settlements.IsEmpty && _workflows.Settlements.IsEmpty).ShouldBeTrue();
}
