// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Specifications;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_StageInvocationHosting;

public class when_composing_the_performers_of_abilities : Specification
{
    sealed class OtherPerformers : IWorkflowNodePerformers
    {
        public IWorkflowNodePerformer? For(CapabilityReference capability) =>
            capability.Id.Value == "stage.send.ChargeFee" ? new Other() : null;

        sealed class Other : IWorkflowNodePerformer
        {
            public CapabilityReference Capability { get; } = new("stage.send.ChargeFee", 1);

            public Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken) =>
                Task.FromResult(WorkflowNodeOutcome.Succeeded("next"));
        }
    }

    IWorkflowNodePerformers _withLifecycles = null!;
    IWorkflowNodePerformers _withoutLifecycles = null!;

    void Because()
    {
        var configuration = new ConfigurationBuilder().Build();
        AbilityDescriptor[] abilities = [new(new("Slack", "Post", 1), EffectClass.Safe, [], "sha256:aaaa")];

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IGrainFactory>());
        services.AddSingleton<IAbilityCatalog>(new AbilityCatalog(abilities));
        services.AddSingleton<IWorkflowNodePerformers>(new OtherPerformers());
        services.AddStageInvocations(configuration).ComposeWithAbilities();
        _withLifecycles = services.BuildServiceProvider().GetRequiredService<IWorkflowNodePerformers>();

        var bare = new ServiceCollection();
        bare.AddLogging();
        bare.AddSingleton(Substitute.For<IGrainFactory>());
        bare.AddSingleton<IAbilityCatalog>(new AbilityCatalog(abilities));
        bare.AddStageInvocations(configuration).ComposeWithAbilities();
        _withoutLifecycles = bare.BuildServiceProvider().GetRequiredService<IWorkflowNodePerformers>();
    }

    [Fact] void should_keep_the_performers_that_were_registered() => _withLifecycles.For(new("stage.send.ChargeFee", 1)).ShouldNotBeNull();
    [Fact] void should_add_the_performers_of_abilities() => _withLifecycles.For(new("ability:Slack.Post", 1)).ShouldNotBeNull();
    [Fact] void should_add_the_performer_that_settles_the_origin() => _withLifecycles.For(new(SettlePerformer.CapabilityId, 1)).ShouldNotBeNull();
    [Fact] void should_serve_abilities_when_no_lifecycle_performers_were_registered() => _withoutLifecycles.For(new("ability:Slack.Post", 1)).ShouldNotBeNull();
    [Fact] void should_not_invent_performers_nobody_registered() => _withoutLifecycles.For(new("stage.send.ChargeFee", 1)).ShouldBeNull();
}
