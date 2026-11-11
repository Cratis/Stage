// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Lifecycles.for_StageWorkflowPerformers;

public class when_a_command_action_reports_the_command_outcome : Specification
{
    WorkflowNodeOutcome _refusalRouted = null!;
    WorkflowNodeOutcome _refusalNotRouted = null!;
    WorkflowNodeOutcome _accepted = null!;
    WorkflowNodeOutcome _unsupported = null!;

    async Task Because()
    {
        var plan = a_lifecycle_source.Plan();
        var runtime = Substitute.For<ISemanticRuntime>();
        var performer = new StageWorkflowPerformers(plan, () => runtime).For(new(WorkflowGraphSpecs.SendCapability("ChargeFee"), 1))!;

        static Task<SemanticExecutionResult> Answer(SemanticExecutionResult result) =>
            Task.FromResult(result);

        runtime.Execute(default!, default!, default!, default!, default).ReturnsForAnyArgs(
            Answer(new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Validation, null, "The invoice is settled.")),
            Answer(new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Validation, null, "The invoice is settled.")),
            Answer(new SemanticAccepted(SemanticWorld.Empty, [], [])),
            Answer(new SemanticUnsupported(SemanticWorld.Empty, default, "not here")));
        _refusalRouted = await performer.Perform(Invocation(WorkflowGraphSpecs.RefusalRoutedConfiguration), CancellationToken.None);
        _refusalNotRouted = await performer.Perform(Invocation("{}"), CancellationToken.None);
        _accepted = await performer.Perform(Invocation("{}"), CancellationToken.None);
        _unsupported = await performer.Perform(Invocation("{}"), CancellationToken.None);
    }

    static WorkflowNodeInvocation Invocation(string configuration) =>
        new(
            new(new("Default"), new("run-1")),
            new(new("ChargeLateFee"), new("root"), 1),
            new(1),
            new("key-1"),
            new(configuration),
            new(new JsonObject { ["trigger"] = new JsonObject { ["subject"] = new JsonObject { ["id"] = a_lifecycle_source.InvoiceId }, ["payload"] = new JsonObject() } }.ToJsonString()),
            []);

    [Fact] void should_leave_through_the_refused_outlet_when_the_model_routes_the_refusal() => (_refusalRouted.Status == WorkflowNodeOutcomeStatus.Succeeded && _refusalRouted.Outlet.Value == "refused").ShouldBeTrue();
    [Fact] void should_fail_the_node_when_nothing_routes_the_refusal() => _refusalNotRouted.Status.ShouldEqual(WorkflowNodeOutcomeStatus.Failed);
    [Fact] void should_say_that_it_was_a_refusal_when_nothing_routes_it() => _refusalNotRouted.Reason.Value.Contains("refused", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_through_the_next_outlet_when_the_command_is_accepted() => _accepted.Outlet.Value.ShouldEqual("next");
    [Fact] void should_report_a_command_that_cannot_run_as_failed_and_not_refused() => (_unsupported.Status == WorkflowNodeOutcomeStatus.Failed && _unsupported.Outlet.Value != "refused").ShouldBeTrue();
}
