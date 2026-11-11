// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityPerformer;

public class when_an_attempt_of_a_once_ability_is_repeated_after_a_restart : a_performer_context
{
    TaskCompletionSource<AbilityResult> _inFlight = null!;
    Task<WorkflowNodeOutcome> _interrupted = null!;
    WorkflowNodeOutcome _repeated = null!;
    InvocationRecord _record = null!;

    protected override EffectClass Effect => EffectClass.Once;

    async Task Because()
    {
        _inFlight = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _backend.Behavior = _ => _inFlight.Task;

        // The first call is in the system when the silo goes down: it never reports back. The wave job that is recovered performs the same attempt again.
        _interrupted = _performer.Perform(Invocation(1), CancellationToken.None);
        await Eventually(() => _backend.Calls.Count == 1, "the first call to reach the extension");
        _repeated = await _performer.Perform(Invocation(1), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
        _record = (await Ledger.List()).Single();
        _inFlight.SetResult(new AbilityReturned());
        await _interrupted;
    }

    [Fact] void should_not_call_the_system_a_second_time() => _backend.Calls.Count.ShouldEqual(1);
    [Fact] void should_answer_that_the_outcome_is_not_known() => _repeated.Status.ShouldEqual(WorkflowNodeOutcomeStatus.Ambiguous);
    [Fact] void should_stop_the_invocation_for_reconciliation() => _record.Status.ShouldEqual(InvocationStatus.Stopped);
    [Fact] void should_say_why_it_was_not_repeated() => _record.Reason.Contains("at most once", StringComparison.Ordinal).ShouldBeTrue();
}
