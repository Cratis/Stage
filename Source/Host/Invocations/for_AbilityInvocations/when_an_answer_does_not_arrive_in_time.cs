// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_an_answer_does_not_arrive_in_time : an_invocation_silo
{
    InvocationRecord _settled = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities =>
        [Describe("Ask", EffectClass.Idempotent, "approved") with { AnswersLater = true, AnswerExpiresAfter = TimeSpan.FromSeconds(1) }];

    async Task Because()
    {
        _backend.Behavior = _ => Task.FromResult<AbilityResult>(new AbilityAccepted());
        await Invocations.Invoke(new("Slack", "Ask", Origin("event 52")));
        await Eventually(() => !_reactions.Settlements.IsEmpty, "the invocation to expire");
        _settled = _reactions.Settlements.Single();
    }

    [Fact] void should_settle_the_origin_with_the_failed_branch() => InvocationBranch.Of(_settled).ShouldEqual("failed");
    [Fact] void should_end_the_invocation_as_failed() => _settled.Status.ShouldEqual(InvocationStatus.Failed);
    [Fact] void should_say_that_no_answer_arrived() => _settled.Reason.Contains("No answer arrived", StringComparison.Ordinal).ShouldBeTrue();
}
