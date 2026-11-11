// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityInvocations;

public class when_invocations_come_from_each_origin : an_invocation_silo
{
    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Post", EffectClass.Safe)];

    async Task Because()
    {
        await Invocations.Invoke(new("Slack", "Post", Origin("event 1", InvocationOriginKind.ReactionBranch)));
        await Invocations.Invoke(new("Slack", "Post", Origin("transition 2", InvocationOriginKind.StateMachineEntry)));
        await Invocations.Invoke(new("Slack", "Post", Origin("run 3/node 1", InvocationOriginKind.WorkflowAction)));
        await Eventually(() => !_reactions.Settlements.IsEmpty && !_stateMachines.Settlements.IsEmpty && !_workflows.Settlements.IsEmpty, "every origin to settle");
    }

    [Fact] void should_settle_the_reaction_branch_with_its_own_invocation() => _reactions.Settlements.Single().Origin.Causation.ShouldEqual("event 1");
    [Fact] void should_complete_the_state_machine_entry_with_its_own_invocation() => _stateMachines.Settlements.Single().Origin.Causation.ShouldEqual("transition 2");
    [Fact] void should_give_the_workflow_action_its_own_invocation() => _workflows.Settlements.Single().Origin.Causation.ShouldEqual("run 3/node 1");
}
