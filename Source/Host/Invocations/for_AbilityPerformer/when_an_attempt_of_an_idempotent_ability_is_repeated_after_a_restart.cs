// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityPerformer;

public class when_an_attempt_of_an_idempotent_ability_is_repeated_after_a_restart : a_performer_context
{
    TaskCompletionSource<AbilityResult> _inFlight = null!;
    Task<WorkflowNodeOutcome> _interrupted = null!;
    WorkflowNodeOutcome _repeated = null!;

    protected override EffectClass Effect => EffectClass.Idempotent;

    async Task Because()
    {
        _inFlight = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = true;
        _backend.Behavior = _ =>
        {
            if (!first) return Task.FromResult<AbilityResult>(new AbilityReturned());
            first = false;
            return _inFlight.Task;
        };

        _interrupted = _performer.Perform(Invocation(1), CancellationToken.None);
        await Eventually(() => _backend.Calls.Count == 1, "the first call to reach the extension");
        _repeated = await _performer.Perform(Invocation(1), CancellationToken.None);
        _inFlight.SetResult(new AbilityReturned());
        await _interrupted;
    }

    [Fact] void should_call_the_system_again() => _backend.Calls.Count.ShouldEqual(2);
    [Fact] void should_call_it_with_the_same_idempotency_key_so_it_can_deduplicate() => _backend.Calls.Select(call => call.IdempotencyKey).Distinct().Count().ShouldEqual(1);
    [Fact] void should_report_the_outcome_of_the_repeated_attempt() => _repeated.Status.ShouldEqual(WorkflowNodeOutcomeStatus.Succeeded);
}
