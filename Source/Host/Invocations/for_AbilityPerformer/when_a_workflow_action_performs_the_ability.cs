// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_AbilityPerformer;

public class when_a_workflow_action_performs_the_ability : an_invocation_silo
{
    AbilityPerformer _performer = null!;
    WorkflowNodeOutcome _first = null!;
    WorkflowNodeOutcome _redelivered = null!;
    InvocationRecord _record = null!;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Charge", EffectClass.Once, "Declined")];

    async Task Because()
    {
        _performer = new(Abilities.Single(), Ledger, _backend, _app.Services.GetRequiredService<SystemGates>(), _app.Services.GetRequiredService<InvocationDurability>(), _app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("spec"));
        _backend.Behavior = _ => Task.FromResult<AbilityResult>(new AbilityReturned("Declined", """{"code":"51"}"""));
        var invocation = new WorkflowNodeInvocation(new(new("Default"), new("run-7")), new(new("Charge"), WorkflowBranchKey.Root, 1), new(1), new("key-7"), new("""{"address":"Collect/Charge"}"""), new("{}"), []);
        _first = await _performer.Perform(invocation, CancellationToken.None);

        // The engine runs the node again after a restart that lost the report.
        _redelivered = await _performer.Perform(invocation, CancellationToken.None);
        _record = (await Ledger.List()).Single();
    }

    [Fact] void should_take_the_outlet_the_outcome_names() => _first.Outlet.Value.ShouldEqual("Declined");
    [Fact] void should_carry_what_the_ability_returned() => _first.Output.Value.ShouldEqual("""{"code":"51"}""");
    [Fact] void should_answer_a_redelivered_node_with_the_recorded_outcome() => _redelivered.Outlet.Value.ShouldEqual("Declined");
    [Fact] void should_not_call_the_system_again() => _backend.Calls.Count.ShouldEqual(1);
    [Fact] void should_identify_the_invocation_by_its_node_execution() => (_record.Origin.Kind == InvocationOriginKind.WorkflowAction && _record.Origin.Address == "Collect/Charge" && _record.Origin.Causation.Contains("run-7", StringComparison.Ordinal)).ShouldBeTrue();
}
