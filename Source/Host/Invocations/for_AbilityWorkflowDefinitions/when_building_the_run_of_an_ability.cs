// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Specifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityWorkflowDefinitions;

public class when_building_the_run_of_an_ability : Specification
{
    WorkflowDefinitionSnapshot _snapshot = null!;
    bool _ownsAnother;

    async Task Because()
    {
        var descriptor = new AbilityDescriptor(new("Slack", "Ask", 2), EffectClass.Once, ["approved"], "sha256:beef")
        {
            MaxAttempts = 4,
            Timeout = TimeSpan.FromSeconds(20),
            AnswersLater = true,
            AnswerExpiresAfter = TimeSpan.FromHours(1)
        };
        var options = new StageInvocationOptions();
        options.Systems["Slack"] = new() { MaxAttempts = 2 };
        using var gates = new SystemGates(Options.Create(options));
        var definitions = new AbilityWorkflowDefinitions(new AbilityCatalog([descriptor]), gates);
        _snapshot = (await definitions.GetActive(new(new("Default"), new("ability:Slack.Ask"))))!;
        _ownsAnother = definitions.Owns(new(new("Default"), new("ability:Slack.Reply")));
    }

    WorkflowNode Node(string name) => _snapshot.Graph.Nodes.Single(node => node.Id.Value == name);

    [Fact] void should_pin_the_package_digest_with_the_capability() => (_snapshot.Capabilities.Single().Provider.Value == "sha256:beef" && _snapshot.Capabilities.Single().Capability.ToString() == "ability:Slack.Ask@2").ShouldBeTrue();
    [Fact] void should_perform_the_ability_under_the_capability_with_the_contract_major() => Node("invoke").Capability.ToString().ShouldEqual("ability:Slack.Ask@2");
    [Fact] void should_declare_the_outlets_of_the_ability() => Node("invoke").Outlets.Select(outlet => outlet.Value).ToArray().ShouldEqual(["returned", "approved", "failed", "expired"]);
    [Fact] void should_narrow_the_attempts_to_the_limit_of_the_system() => Node("invoke").Policy.MaxAttempts.ShouldEqual(2);
    [Fact] void should_give_an_attempt_the_time_the_ability_declares() => Node("invoke").Policy.AttemptTimeout.ShouldEqual(TimeSpan.FromSeconds(20));
    [Fact] void should_settle_the_origin_after_every_outlet() => _snapshot.Graph.Edges.Count(edge => edge.From.Value == "invoke").ShouldEqual(4);
    [Fact] void should_own_only_the_workflows_of_its_abilities() => _ownsAnother.ShouldBeFalse();
}
