// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_an_ability_ends_in_a_named_outcome : an_invocation_silo
{
    InvocationRecord _settled = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Reserve", EffectClass.Idempotent, "OutOfStock")];

    async Task Because()
    {
        _backend.Behavior = _ => Task.FromResult<AbilityResult>(new AbilityReturned("OutOfStock"));
        await Invocations.Invoke(new("Slack", "Reserve", Origin("event 7")));
        await Eventually(() => !_reactions.Settlements.IsEmpty, "the reaction branch to settle");
        _settled = _reactions.Settlements.Single();
    }

    [Fact] void should_settle_the_origin_with_the_branch_of_the_outcome() => InvocationBranch.Of(_settled).ShouldEqual("OutOfStock");
    [Fact] void should_end_the_invocation_as_settled() => _settled.Status.ShouldEqual(InvocationStatus.Settled);
}
