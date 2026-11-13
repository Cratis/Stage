// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Xunit;

namespace Cratis.Stage.Contracts.Lifecycles.for_WorkflowGraphSpecs;

public class when_a_workflow_action_invokes_an_ability : Specification
{
    const string Action = "action ChargeLateFee invokes ChargeFee attempts 3 timeout 30 seconds retry after 5 seconds";

    WorkflowGraphSpec _graph = null!;
    WorkflowGraphNodeSpec _node = null!;
    JsonObject _configuration = null!;

    void Because()
    {
        const string System = "system Billing\n  ability Charge\n    reason String\n    overdue Bool\n    outcome Declined\n    answers later\n\n";
        const string Invocation = "action ChargeLateFee invokes Billing.Charge\n    reason = \"late\"\n    overdue = overdue";
        var source = System + a_lifecycle_source.Source.Replace(Action, Invocation, StringComparison.Ordinal);
        if (source.Contains(Action, StringComparison.Ordinal)) throw new InvalidOperationException("The lifecycle source no longer has the action this specification turns into an ability invocation.");
        _graph = WorkflowGraphSpecs.For(a_lifecycle_source.Plan(source)).Single(graph => graph.Workflow.Name == "Collect");
        _node = _graph.Nodes.Single(node => node.Name == "ChargeLateFee");
        _configuration = JsonNode.Parse(_node.ConfigurationJson)!.AsObject();
    }

    [Fact] void should_not_refuse_the_workflow() => _graph.Refusals.ShouldBeEmpty();
    [Fact] void should_perform_it_under_the_ability_capability() => _node.Capability.ShouldEqual("ability:Billing.Charge");
    [Fact] void should_leave_through_what_the_ability_can_end_in() => _node.Outlets.ShouldContainOnly("returned", "Declined", "failed", "expired");
    [Fact] void should_name_the_action_as_the_address() => _configuration["address"]!.GetValue<string>().ShouldEqual("Collect/ChargeLateFee");
    [Fact] void should_describe_the_literal_input() => _configuration["inputs"]![0]!["value"]!.GetValue<string>().ShouldEqual("late");
    [Fact] void should_describe_the_input_read_from_the_subject() => _configuration["inputs"]![1]!["subject"]!.GetValue<string>().ShouldEqual("overdue");
    [Fact] void should_route_next_to_the_returned_outlet() => _graph.Edges.Any(edge => edge.From == "ChargeLateFee" && edge.Outlet == "returned" && edge.To == "Pause").ShouldBeTrue();
}
