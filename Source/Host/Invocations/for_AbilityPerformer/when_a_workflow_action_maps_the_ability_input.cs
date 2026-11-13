// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityPerformer;

public class when_a_workflow_action_maps_the_ability_input : an_invocation_silo
{
    JsonObject _input = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Charge", EffectClass.Safe)];

    async Task Because()
    {
        var performer = new AbilityPerformer(Abilities.Single(), Ledger, _backend, _app.Services.GetRequiredService<SystemGates>(), _app.Services.GetRequiredService<InvocationDurability>(), _app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("spec"));
        _backend.Behavior = _ => Task.FromResult<AbilityResult>(new AbilityReturned(string.Empty, "{}"));
        const string Configuration = """
            {"address":"Collect/Charge","inputs":[{"input":"reason","value":"late"},{"input":"overdue","subject":"overdue"},{"input":"booking","node":"Book","field":"bookingId"}]}
            """;
        const string Input = """
            {"trigger":{"payload":{"overdue":true,"status":"sent"}},"nodes":{"Book":{"bookingId":"B-1"}}}
            """;
        var invocation = new WorkflowNodeInvocation(new(new("Default"), new("run-8")), new(new("Charge"), WorkflowBranchKey.Root, 1), new(1), new("key-8"), new(Configuration), new(Input), []);
        await performer.Perform(invocation, CancellationToken.None);
        _input = JsonNode.Parse(_backend.Calls.Single().Input)!.AsObject();
    }

    [Fact] void should_give_the_literal() => _input["reason"]!.GetValue<string>().ShouldEqual("late");
    [Fact] void should_read_the_subject_the_run_started_for() => _input["overdue"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_read_the_field_an_earlier_node_returned() => _input["booking"]!.GetValue<string>().ShouldEqual("B-1");
    [Fact] void should_give_only_what_the_model_maps() => _input.Count.ShouldEqual(3);
}
