// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Triggers;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_SignalContractTriggers;

public class when_signalling_a_trigger : Specification
{
    IWorkflows _workflows = null!;
    IContractTriggerRoutes _routes = null!;
    SignalContractTriggers _triggers = null!;
    List<(string Workflow, WorkflowTrigger Trigger)> _started = null!;
    TriggerRaised _raised = null!;
    TriggerRaised _withoutKey = null!;
    TriggerRaised _nothingReacts = null!;
    TriggerRaised _rejected = null!;

    void Establish()
    {
        _started = [];
        _workflows = Substitute.For<IWorkflows>();
        _workflows.Start(Arg.Any<WorkflowScope>(), Arg.Any<WorkflowId>(), Arg.Any<WorkflowTrigger>()).Returns(call =>
        {
            var workflow = call.ArgAt<WorkflowId>(1).Value;
            _started.Add((workflow, call.Arg<WorkflowTrigger>()));
            return workflow == "Broken" ? new WorkflowAdmission(false, new WorkflowRunId("none"), [new WorkflowAdmissionRejection("no active version")]) : WorkflowAdmission.To(new WorkflowRunId($"run-of-{workflow}"));
        });
        _routes = Substitute.For<IContractTriggerRoutes>();
        _routes.WorkflowsFor("Slack", "MessageReceived").Returns(["OnMessage", "Audit"]);
        _routes.WorkflowsFor("Slack", "ChannelArchived").Returns(["Broken"]);
        _routes.WorkflowsFor("Slack", "ReactionAdded").Returns([]);
        _triggers = new SignalContractTriggers(_routes, _workflows);
    }

    async Task Because()
    {
        _raised = await _triggers.Signal(new("Slack", "MessageReceived", "Default", "msg-1", "{\"text\":\"hi\"}", "invocation-1"));
        _withoutKey = await _triggers.Signal(new("Slack", "MessageReceived", "Default", string.Empty, "{}", "x"));
        _nothingReacts = await _triggers.Signal(new("Slack", "ReactionAdded", "Default", "r-1", "{}", "x"));
        _rejected = await _triggers.Signal(new("Slack", "ChannelArchived", "Default", "c-1", "{}", "x"));
    }

    static bool IsReaction(string workflow) => string.Equals(workflow, "OnMessage", StringComparison.Ordinal) || string.Equals(workflow, "Audit", StringComparison.Ordinal);

    [Fact] void should_raise_it() => _raised.Status.ShouldEqual(TriggerRaiseStatus.Raised);
    [Fact] void should_start_every_workflow_that_reacts() => _started.Where(start => IsReaction(start.Workflow)).Select(start => start.Workflow).Order().ToArray().ShouldEqual(["Audit", "OnMessage"]);
    [Fact] void should_start_them_as_a_signal() => _started[0].Trigger.Kind.ShouldEqual(WorkflowTriggerKind.Signal);
    [Fact] void should_deduplicate_each_start_on_the_trigger_the_key_and_the_workflow() => _started.Where(start => IsReaction(start.Workflow)).Select(start => start.Trigger.Deduplication.Value).Order().ToArray().ShouldEqual(["Slack.MessageReceived/msg-1/Audit", "Slack.MessageReceived/msg-1/OnMessage"]);
    [Fact] void should_hand_the_payload_to_the_workflow() => _started[0].Trigger.Payload.Value.ShouldEqual("{\"text\":\"hi\"}");
    [Fact] void should_name_the_cause() => _started[0].Trigger.Causation.Value.ShouldEqual("invocation-1");
    [Fact] void should_refuse_a_trigger_without_a_deduplication_key() => _withoutKey.Status.ShouldEqual(TriggerRaiseStatus.Refused);
    [Fact] void should_report_a_trigger_nothing_reacts_to() => (_nothingReacts.Status == TriggerRaiseStatus.NotRouted && _nothingReacts.Detail.Contains("Slack.ReactionAdded", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_refuse_when_the_engine_does_not_admit_a_run() => (_rejected.Status == TriggerRaiseStatus.Refused && _rejected.Detail.Contains("no active version", StringComparison.Ordinal)).ShouldBeTrue();
}
