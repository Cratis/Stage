// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_reconciling_before_the_performer_reports_ambiguity : an_invocation_silo
{
    readonly TaskCompletionSource _paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    InvocationRecord _stopped = null!;
    InvocationRecord _settled = null!;
    NodeExecutionStatus _before;
    InvocationReconciliation _reconciled;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Pay", EffectClass.Once, "Declined")];

    protected override void Configure(WebApplicationBuilder builder) =>
        builder.Host.ConfigureContainer<IServiceCollection>((_, services) => services.AddSingleton<IWorkflowNodePerformers>(provider =>
            new PausedPerformers(provider.GetRequiredService<AbilityWorkflowPerformers>(), _paused, _release.Task)));

    async Task Because()
    {
        _backend.Behavior = _ => throw new TimeoutException("no answer");
        var admission = await Invocations.Invoke(new("Slack", "Pay", Origin("event 23", InvocationOriginKind.StateMachineEntry)));
        try
        {
            // The real performer has persisted Stopped, but cannot return Ambiguous to the engine yet.
            await _paused.Task.WaitAsync(TimeSpan.FromSeconds(10));
            _stopped = (await Ledger.Get(admission.Record!.Identity))!;
            var key = WorkflowRunKey.Parse(_stopped.Run);
            var execution = NodeExecutionKey.Parse(_stopped.Execution);
            var run = _app.Services.GetRequiredService<IWorkflows>().GetRun(key.Scope, key.Run);
            _before = (await run.GetSnapshot()).Executions.Single(candidate => candidate.Execution == execution).Status;
            _before.ShouldEqual(NodeExecutionStatus.Scheduled);
            _stopped.Status.ShouldEqual(InvocationStatus.Stopped);

            _reconciled = await Invocations.Reconcile(_stopped.Identity, "Declined", "{}", "alice");
            _reconciled.ShouldEqual(InvocationReconciliation.Reconciled);
        }
        finally
        {
            _release.TrySetResult();
        }

        await Eventually(() => !_stateMachines.Settlements.IsEmpty, "the state machine to be settled");
        _settled = _stateMachines.Settlements.Single();
    }

    [Fact] void should_exercise_the_gap_before_workflow_readiness() => _before.ShouldEqual(NodeExecutionStatus.Scheduled);
    [Fact] void should_already_advertise_the_invocation_as_stopped() => _stopped.Status.ShouldEqual(InvocationStatus.Stopped);
    [Fact] void should_reconcile_the_stopped_invocation_in_one_call() => _reconciled.ShouldEqual(InvocationReconciliation.Reconciled);
    [Fact] void should_continue_the_origin_with_the_chosen_outcome() => InvocationBranch.Of(_settled).ShouldEqual("Declined");
    [Fact] void should_record_who_chose() => _settled.DecidedBy.ShouldEqual("alice");
    [Fact] void should_not_repeat_the_effect() => _backend.Calls.Count.ShouldEqual(1);
    [Fact] void should_settle_the_origin_once() => _stateMachines.Settlements.Count.ShouldEqual(1);

    sealed class PausedPerformers(IWorkflowNodePerformers inner, TaskCompletionSource paused, Task release) : IWorkflowNodePerformers
    {
        public IWorkflowNodePerformer? For(CapabilityReference capability) =>
            inner.For(capability) is { } performer ? new PausedPerformer(performer, paused, release) : null;
    }

    sealed class PausedPerformer(IWorkflowNodePerformer inner, TaskCompletionSource paused, Task release) : IWorkflowNodePerformer
    {
        public CapabilityReference Capability => inner.Capability;

        public async Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken)
        {
            var outcome = await inner.Perform(invocation, cancellationToken);
            if (outcome.Status == WorkflowNodeOutcomeStatus.Ambiguous)
            {
                paused.TrySetResult();
                await release.WaitAsync(cancellationToken);
            }

            return outcome;
        }
    }
}
