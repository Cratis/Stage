// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityPerformer;

public class when_a_workflow_action_was_certainly_not_sent : an_invocation_silo
{
    WorkflowNodeOutcome _outcome = null!;
    InvocationRecord _record = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Charge", EffectClass.Once)];

    async Task Because()
    {
        var performer = new AbilityPerformer(Abilities.Single(), Ledger, _backend, _app.Services.GetRequiredService<SystemGates>(), _app.Services.GetRequiredService<InvocationDurability>(), _app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("spec"));
        _backend.Behavior = _ => Task.FromResult<AbilityResult>(new AbilityNotSent("no extension is connected yet"));
        var invocation = new WorkflowNodeInvocation(new(new("Default"), new("run-9")), new(new("Charge"), WorkflowBranchKey.Root, 1), new(1), new("key-9"), new("""{"address":"Collect/Charge","maxAttempts":1}"""), new("{}"), []);
        _outcome = await performer.Perform(invocation, CancellationToken.None);
        await performer.Perform(invocation, CancellationToken.None);
        _record = (await Ledger.List()).Single();
    }

    [Fact] void should_fail_the_node() => _outcome.Status.ShouldEqual(WorkflowNodeOutcomeStatus.Failed);
    [Fact] void should_preserve_why_the_node_failed() => _outcome.Reason.Value.ShouldEqual("no extension is connected yet");
    [Fact] void should_fail_the_ledger_on_the_nodes_last_attempt() => _record.Status.ShouldEqual(InvocationStatus.Failed);
    [Fact] void should_record_when_it_settled() => _record.Settled.ShouldNotBeNull();
    [Fact] void should_preserve_why_it_failed() => _record.Reason.ShouldEqual("no extension is connected yet");
    [Fact] void should_not_call_the_system_again_on_redelivery() => _backend.Calls.Count.ShouldEqual(1);
}
