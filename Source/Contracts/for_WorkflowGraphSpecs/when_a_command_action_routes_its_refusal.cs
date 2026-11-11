// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Xunit;

namespace Cratis.Stage.Contracts.Lifecycles.for_WorkflowGraphSpecs;

public class when_a_command_action_routes_its_refusal : Specification
{
    WorkflowGraphSpec _graph = null!;
    WorkflowGraphSpec _unrouted = null!;

    void Because()
    {
        const string Unrouted = "    failed -> Approve\n  wait Pause";
        var source = a_lifecycle_source.Source.Replace(Unrouted, "    failed -> Approve\n    refused -> Plan\n  wait Pause", StringComparison.Ordinal);
        if (source == a_lifecycle_source.Source) throw new InvalidOperationException("The lifecycle source no longer has the action this specification routes a refusal from.");
        _graph = WorkflowGraphSpecs.For(a_lifecycle_source.Plan(source)).Single(graph => graph.Workflow.Name == "Collect");
        _unrouted = WorkflowGraphSpecs.For(a_lifecycle_source.Plan()).Single(graph => graph.Workflow.Name == "Collect");
    }

    WorkflowGraphNodeSpec Action(WorkflowGraphSpec graph, string name) => graph.Nodes.Single(node => node.Name == name);

    [Fact] void should_declare_the_refused_outlet_on_every_command_action() => (Action(_graph, "ChargeLateFee").Outlets.Contains("refused") && Action(_graph, "TagLine").Outlets.Contains("refused")).ShouldBeTrue();
    [Fact] void should_connect_the_refused_outlet_the_model_routes() => _graph.Edges.Any(edge => edge.From == "ChargeLateFee" && edge.Outlet == "refused" && edge.To == "Plan").ShouldBeTrue();
    [Fact] void should_tell_the_action_its_refusal_is_routed() => Action(_graph, "ChargeLateFee").ConfigurationJson.ShouldEqual(WorkflowGraphSpecs.RefusalRoutedConfiguration);
    [Fact] void should_leave_the_configuration_alone_when_the_model_does_not_route_it() => Action(_unrouted, "ChargeLateFee").ConfigurationJson.ShouldEqual("{}");
    [Fact] void should_keep_failure_and_refusal_apart() => Action(_graph, "ChargeLateFee").Outlets.Intersect(["failed", "refused"]).Count().ShouldEqual(2);
}
