// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityWorkflowPerformers;

public class when_asking_for_a_performer : an_invocation_silo
{
    AbilityWorkflowPerformers _performers = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Post", EffectClass.Safe)];

    void Because() => _performers = _app.Services.GetRequiredService<AbilityWorkflowPerformers>();

    [Fact] void should_find_the_performer_of_the_ability_at_its_contract_major() => _performers.For(new("ability:Slack.Post", 1)).ShouldNotBeNull();
    [Fact] void should_not_find_a_performer_at_another_contract_major() => _performers.For(new("ability:Slack.Post", 2)).ShouldBeNull();
    [Fact] void should_not_find_an_ability_the_model_does_not_declare() => _performers.For(new("ability:Slack.Reply", 1)).ShouldBeNull();
    [Fact] void should_find_the_performer_that_settles_the_origin() => _performers.For(new(SettlePerformer.CapabilityId, 1)).ShouldNotBeNull();
}
