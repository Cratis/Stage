// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Invocations;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_GrpcAbilityBackend;

public class when_a_backend_answers_an_attempt : an_extension_protocol
{
    AbilityResult _plain = null!;
    AbilityResult _accepted = null!;
    AbilityResult _notSent = null!;
    AbilityResult _failed = null!;
    AbilityResult _failedWithEffectRuledOut = null!;
    AbilityResult _nothing = null!;

    async Task Establish()
    {
        await StartBackend();
        await StartStage();
    }

    async Task Because()
    {
        _plain = await Answer(new InvokeResponse { Returned = new Returned() });
        _accepted = await Answer(new InvokeResponse { Accepted = new Accepted() });
        _notSent = await Answer(new InvokeResponse { NotSent = new NotSent { Reason = "rate limited before sending" } });
        _failed = await Answer(new InvokeResponse { Failed = new Failed { Reason = "timed out" } });
        _failedWithEffectRuledOut = await Answer(new InvokeResponse { Failed = new Failed { Reason = "rejected", EffectRuledOut = true } });
        _nothing = await Answer(new InvokeResponse());
    }

    async Task<AbilityResult> Answer(InvokeResponse response)
    {
        _backend.OnInvoke = _ => response;
        return await _abilities.Invoke(Call(), CancellationToken.None);
    }

    [Fact] void should_return_with_no_outcome_for_a_plain_return() => ((AbilityReturned)_plain).Outcome.ShouldBeNull();
    [Fact] void should_return_an_empty_object_when_the_backend_returned_nothing() => ((AbilityReturned)_plain).Output.ShouldEqual("{}");
    [Fact] void should_be_accepted_to_answer_later() => (_accepted is AbilityAccepted).ShouldBeTrue();
    [Fact] void should_not_have_sent_it_when_the_backend_says_so() => ((AbilityNotSent)_notSent).Reason.ShouldEqual("rate limited before sending");
    [Fact] void should_assume_the_effect_may_have_happened_when_it_failed() => ((AbilityFailed)_failed).MayHaveReachedTheSystem.ShouldBeTrue();
    [Fact] void should_say_it_did_not_reach_the_system_only_when_the_backend_ruled_it_out() => ((AbilityFailed)_failedWithEffectRuledOut).MayHaveReachedTheSystem.ShouldBeFalse();
    [Fact] void should_treat_an_empty_answer_as_a_failure_that_may_have_reached_the_system() => ((AbilityFailed)_nothing).MayHaveReachedTheSystem.ShouldBeTrue();
}
