// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Xunit;

namespace Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission;

public class when_admitting_the_lifecycles_of_a_model : Specification
{
    Cratis.Screenplay.Semantics.Execution.SemanticExecutionPlan _plan = null!;
    WorkflowGraphSpec _graph = null!;

    void Establish() => _plan = a_lifecycle_source.Plan();

    void Because() => _graph = WorkflowGraphSpecs.For(_plan).Single();

    [Fact] void should_refuse_nothing() => LifecycleAdmission.ForRuntime(_plan).ShouldBeEmpty();
    [Fact] void should_run_the_workflow() => LifecycleAdmission.ExecutableWorkflows(_plan).ShouldContainOnly(["Collect"]);
    [Fact] void should_translate_every_node() => _graph.Nodes.Select(node => node.Name).ShouldContainOnly(["Trigger", "ChargeLateFee", "Pause", "Check", "EachLine", "TagLine", "AllTagged", "Await", "Approve", "Write", "Plan", "Done"]);
    [Fact] void should_leave_a_timer_by_the_elapsed_outlet() => _graph.Edges.Any(edge => edge.From == "Pause" && edge.Outlet == "elapsed" && edge.To == "Check").ShouldBeTrue();
    [Fact] void should_continue_after_the_fan_out_from_its_join() => _graph.Edges.Any(edge => edge.From == "AllTagged" && edge.Outlet == "completed" && edge.To == "Await").ShouldBeTrue();
    [Fact] void should_key_the_join_by_its_foreach() => _graph.Nodes.Single(node => node.Name == "AllTagged").ConfigurationJson.Contains("\"forEach\":\"EachLine\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_read_the_items_from_the_trigger() => _graph.Nodes.Single(node => node.Name == "EachLine").ConfigurationJson.Contains("\"itemsFrom\":\"Trigger\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_event_a_wait_waits_for() => _graph.Nodes.Single(node => node.Name == "Await").ConfigurationJson.Contains("\"id\":\"InvoicePaid\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_attempts_of_an_action() => _graph.Nodes.Single(node => node.Name == "ChargeLateFee").MaxAttempts.ShouldEqual(3);
}
