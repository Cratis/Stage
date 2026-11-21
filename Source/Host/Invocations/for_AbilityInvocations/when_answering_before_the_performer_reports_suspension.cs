// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Waits;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_answering_before_the_performer_reports_suspension : an_invocation_silo
{
    readonly TaskCompletionSource _paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    InvocationRecord _waiting = null!;
    InvocationRecord _settled = null!;
    WorkflowRunSnapshot _before = null!;
    WorkflowRunSnapshot _after = null!;
    InvocationAnswer _answered;
    InvocationAnswer _again;

    protected override IEnumerable<AbilityDescriptor> Abilities =>
        [Describe("Ask", EffectClass.Idempotent, "approved", "rejected") with { AnswersLater = true, AnswerExpiresAfter = TimeSpan.FromMinutes(5) }];

    protected override void Configure(WebApplicationBuilder builder) =>
        builder.Host.ConfigureContainer<IServiceCollection>((_, services) => services.AddSingleton<IWorkflowNodePerformers>(provider =>
            new PausedPerformers(provider.GetRequiredService<AbilityWorkflowPerformers>(), _paused, _release.Task)));

    async Task Because()
    {
        _backend.Behavior = _ => Task.FromResult<AbilityResult>(new AbilityAccepted());
        var admission = await Invocations.Invoke(new("Slack", "Ask", Origin("event 52", InvocationOriginKind.WorkflowAction), "{}", "Managers"));
        var identity = admission.Record?.Identity ?? throw new InvalidOperationException("The invocation was not admitted.");
        var person = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Name, "maria"), new(ClaimTypes.Role, "manager")], "spec"));
        try
        {
            // The ledger advertises Suspended, but the performer cannot report it to the engine yet.
            await _paused.Task.WaitAsync(TimeSpan.FromSeconds(10));
            _waiting = await Ledger.Get(identity) ?? throw new InvalidOperationException("The invocation was not recorded.");
            var key = WorkflowRunKey.Parse(_waiting.Run);
            var execution = NodeExecutionKey.Parse(_waiting.Execution);
            var run = _app.Services.GetRequiredService<IWorkflows>().GetRun(key.Scope, key.Run);
            _before = await run.GetSnapshot();

            _answered = await Invocations.Answer(_waiting.Identity, "approved", """{"note":"ok"}""", person);

            // A later report of the same suspended outcome must not reopen the answered wait.
            await run.CompleteNode(execution, new(_waiting.Attempts), WorkflowNodeOutcome.Suspended(new WorkflowCompletionKey(_waiting.Identity), Abilities.Single().AnswerExpiresAfter));
            _after = await run.GetSnapshot();
        }
        finally
        {
            _release.TrySetResult();
        }

        await Eventually(() => !_workflows.Settlements.IsEmpty, "the workflow to take its outlet");
        _settled = _workflows.Settlements.Single();
        _again = await Invocations.Answer(_waiting.Identity, "rejected", "{}", person);
    }

    [Fact] void should_advertise_the_invocation_as_waiting() => _waiting.Status.ShouldEqual(InvocationStatus.Suspended);
    [Fact] void should_exercise_the_gap_before_the_wait_is_registered() => _before.Executions.Single(candidate => candidate.Execution == NodeExecutionKey.Parse(_waiting.Execution)).Status.ShouldEqual(NodeExecutionStatus.Scheduled);
    [Fact] void should_have_no_wait_registered_before_the_answer() => _before.OpenWaits.ShouldBeEmpty();
    [Fact] void should_take_the_early_answer_in_one_call() => _answered.ShouldEqual(InvocationAnswer.Answered);
    [Fact] void should_continue_the_origin_through_the_answered_outlet() => InvocationBranch.Of(_settled).ShouldEqual("approved");
    [Fact] void should_carry_the_answer() => _settled.Output.ShouldEqual("""{"note":"ok"}""");
    [Fact] void should_record_who_answered() => _settled.DecidedBy.ShouldEqual("maria");
    [Fact] void should_not_reopen_the_wait_on_the_late_report() => _after.OpenWaits.ShouldBeEmpty();
    [Fact] void should_not_call_the_extension_again() => _backend.Calls.Count.ShouldEqual(1);
    [Fact] void should_not_take_a_second_answer() => _again.ShouldEqual(InvocationAnswer.NotWaiting);
    [Fact] void should_settle_the_origin_once() => _workflows.Settlements.Count.ShouldEqual(1);

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
            if (outcome.Status == WorkflowNodeOutcomeStatus.Suspended)
            {
                paused.TrySetResult();
                await release.WaitAsync(cancellationToken);
            }

            return outcome;
        }
    }
}
