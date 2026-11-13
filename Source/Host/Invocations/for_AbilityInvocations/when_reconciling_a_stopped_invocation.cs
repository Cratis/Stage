// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_reconciling_a_stopped_invocation : an_invocation_silo
{
    string _identity = null!;
    InvocationReconciliation _unknownOutcome;
    InvocationReconciliation _unknownInvocation;
    InvocationReconciliation _reconciled;
    InvocationReconciliation _again;
    InvocationRecord _settled = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Pay", EffectClass.Once, "Declined")];

    async Task Because()
    {
        _backend.Behavior = _ => throw new TimeoutException("no answer");
        var admission = await Invocations.Invoke(new("Slack", "Pay", Origin("event 21", InvocationOriginKind.StateMachineEntry)));
        _identity = admission.Record!.Identity;
        await Eventually(async () => (await Invocations.List(InvocationStatus.Stopped)).Count == 1, "the invocation to stop");
        var record = (await Ledger.Get(_identity))!;
        var key = WorkflowRunKey.Parse(record.Run);
        var execution = NodeExecutionKey.Parse(record.Execution);
        var run = _app.Services.GetRequiredService<IWorkflows>().GetRun(key.Scope, key.Run);
        await Eventually(async () => (await run.GetSnapshot()).Executions.Any(candidate => candidate.Execution == execution && candidate.Status == NodeExecutionStatus.ReconciliationRequired), "the workflow execution to be ready for reconciliation");

        _unknownOutcome = await Invocations.Reconcile(_identity, "Exploded", "{}", "alice");
        _unknownInvocation = await Invocations.Reconcile("inv-nothing", "returned", "{}", "alice");
        _reconciled = await Invocations.Reconcile(_identity, "Declined", """{"reason":"insufficient funds"}""", "alice");
        _reconciled.ShouldEqual(InvocationReconciliation.Reconciled);
        await Eventually(() => !_stateMachines.Settlements.IsEmpty, "the state machine to be settled");
        _settled = _stateMachines.Settlements.Single();
        _again = await Invocations.Reconcile(_identity, "returned", "{}", "bob");
    }

    [Fact] void should_refuse_an_outcome_the_ability_does_not_declare() => _unknownOutcome.ShouldEqual(InvocationReconciliation.UnknownOutcome);
    [Fact] void should_not_know_an_invocation_that_was_never_recorded() => _unknownInvocation.ShouldEqual(InvocationReconciliation.Unknown);
    [Fact] void should_record_the_outcome_that_happened() => _reconciled.ShouldEqual(InvocationReconciliation.Reconciled);
    [Fact] void should_continue_the_origin_from_that_choice() => InvocationBranch.Of(_settled).ShouldEqual("Declined");
    [Fact] void should_carry_what_the_operator_recorded() => _settled.Output.ShouldEqual("""{"reason":"insufficient funds"}""");
    [Fact] void should_record_who_chose() => _settled.DecidedBy.ShouldEqual("alice");
    [Fact] void should_not_call_the_extension_again() => _backend.Calls.Count.ShouldEqual(1);
    [Fact] void should_not_reconcile_twice() => _again.ShouldEqual(InvocationReconciliation.NotStopped);
    [Fact] void should_settle_the_origin_once() => _stateMachines.Settlements.Count.ShouldEqual(1);
}
