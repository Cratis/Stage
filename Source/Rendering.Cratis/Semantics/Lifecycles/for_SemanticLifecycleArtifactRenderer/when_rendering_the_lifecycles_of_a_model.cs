// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Lifecycles.for_SemanticLifecycleArtifactRenderer;

public class when_rendering_the_lifecycles_of_a_model : Specification
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _refused = null!;

    void Because()
    {
        _plan = invoice_model.Plan(invoice_model.Compile(a_lifecycle_source.RenderableSource));
        _refused = invoice_model.Plan(invoice_model.Compile(a_lifecycle_source.Source));
    }

    string Text(string path) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());

    [Fact]
    void should_compile_against_the_orleans_assemblies() => RenderedOutput.Errors([.. _plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))]).ShouldBeEmpty();
    [Fact]
    void should_compile_without_warnings() => RenderedOutput.Warnings([.. _plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))]).ShouldBeEmpty();
    [Fact] void should_plan_the_application() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Severity}: {diagnostic.Message}")));
    [Fact] void should_render_the_state_machine_as_an_event_sourced_grain() => Text("Lifecycles/InvoiceLifecycle.cs").ShouldContain("public class InvoiceLifecycle : EventSourcedStateMachine<");
    [Fact] void should_declare_the_transitions_in_order_with_their_guard() => Text("Lifecycles/InvoiceLifecycle.cs").ShouldContain(".When(model => model.Overdue == true).TransitionTo<Overdue>()");
    [Fact] void should_allow_the_targets_declared_under_a_state() => Text("Lifecycles/InvoiceLifecycle.cs").ShouldContain("AllowedTransitions => [typeof(Overdue), typeof(Paid)]");
    [Fact] void should_send_the_command_when_entering_a_state() => Text("Lifecycles/InvoiceLifecycle.cs").ShouldContain("Execute(new ChargeFee(new InvoiceId(Guid.Parse(key))))");
    [Fact] void should_declare_each_transition_once() => Text("Lifecycles/InvoiceLifecycle.cs").Split("On<InvoicePaid>()").Length.ShouldEqual(2);
    [Fact] void should_start_the_workflow_when_entering_a_state() => Text("Lifecycles/InvoiceLifecycle.cs").ShouldContain("StartWorkflow(this, \"Collect\")");
    [Fact] void should_render_the_workflow_as_a_definition() => Text("Lifecycles/CollectWorkflow.cs").ShouldContain("IWorkflowDefinitions");
    [Fact] void should_render_a_timer_wait() => Text("Lifecycles/CollectWorkflow.cs").ShouldContain("workflows.wait.timer");
    [Fact] void should_render_one_performer_per_command() => Text("Lifecycles/WorkflowActions.cs").ShouldContain("class SendChargeFeeAction");
    [Fact] void should_signal_the_waits_for_an_event() => Text("Lifecycles/WorkflowSignals.cs").ShouldContain("class InvoicePaidWorkflowSignal");
    [Fact] void should_register_the_event_triggers() => Text("Lifecycles/GeneratedWorkflows.cs").ShouldContain("options.OnEvent<");
    [Fact] void should_report_the_state_after_a_restart() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == LifecycleCodes.RestartState).ShouldBeTrue();
    [Fact] void should_refuse_what_it_cannot_render() => _refused.Success.ShouldBeFalse();
    [Fact] void should_name_the_refused_constructs() => _refused.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-LIFECYCLE-010").Select(diagnostic => diagnostic.Message).ShouldNotBeEmpty();
}
