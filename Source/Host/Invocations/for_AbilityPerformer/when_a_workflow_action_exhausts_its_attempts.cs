// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityPerformer;

public class when_a_workflow_action_exhausts_its_attempts : an_invocation_silo
{
    InvocationStatus _after_first_attempt;
    InvocationRecord _record = null!;
    WorkflowNodeOutcome _outcome = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Post", EffectClass.Idempotent)];

    async Task Because()
    {
        var performer = new AbilityPerformer(Abilities.Single(), Ledger, _backend, _app.Services.GetRequiredService<SystemGates>(), _app.Services.GetRequiredService<InvocationDurability>(), _app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("spec"));
        _backend.Behavior = _ => Task.FromResult<AbilityResult>(new AbilityFailed("the connection was lost"));
        var invocation = new WorkflowNodeInvocation(new(new("Default"), new("run-9")), new(new("Post"), WorkflowBranchKey.Root, 1), new(1), new("key-9"), new("""{"address":"Notify/Post","maxAttempts":2}"""), new("{}"), []);
        await performer.Perform(invocation, CancellationToken.None);
        _after_first_attempt = (await Ledger.List()).Single().Status;
        _outcome = await performer.Perform(invocation with { Attempt = new(2) }, CancellationToken.None);
        _record = (await Ledger.List()).Single();
    }

    [Fact] void should_keep_retrying_while_the_node_has_an_attempt() => _after_first_attempt.ShouldEqual(InvocationStatus.Retrying);
    [Fact] void should_fail_the_last_attempt() => _outcome.Status.ShouldEqual(WorkflowNodeOutcomeStatus.Failed);
    [Fact] void should_preserve_why_the_node_failed() => _outcome.Reason.Value.ShouldEqual("the connection was lost");
    [Fact] void should_fail_the_ledger_before_the_descriptor_exhausts_its_attempts() => _record.Status.ShouldEqual(InvocationStatus.Failed);
    [Fact] void should_record_when_it_settled() => _record.Settled.ShouldNotBeNull();
    [Fact] void should_keep_one_identity_for_all_attempts() => _backend.Calls.Select(call => call.IdempotencyKey).Distinct().Count().ShouldEqual(1);
    [Fact] void should_try_twice() => _backend.Calls.Count.ShouldEqual(2);
}
