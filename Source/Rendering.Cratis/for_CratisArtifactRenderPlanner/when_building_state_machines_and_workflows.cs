// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_building_state_machines_and_workflows(when_building_state_machines_and_workflows.context fixture) : IClassFixture<when_building_state_machines_and_workflows.context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldBeEmpty();
    [Fact] void should_run_generated_specs() => fixture.Results.Select(result => result.Outcome).ShouldContainOnly(Enumerable.Repeat("Passed", 4).ToArray());

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => a_lifecycle_source.RenderableSource;

        async Task Because()
        {
            AddGeneratedSpecification("when_loading_the_generated_lifecycles.cs", """
                #if DEBUG
                using Cratis.Choreography.Workflows;
                using Cratis.Choreography.Workflows.Definitions;
                using Cratis.Choreography.Workflows.Graphs;
                using Cratis.Specifications;
                using Invoices.Lifecycles;
                using Xunit;

                namespace Invoices;

                public class when_loading_the_generated_lifecycles : Specification
                {
                    WorkflowDefinitionSnapshot? _collect;
                    WorkflowDefinitionSnapshot? _nudge;
                    WorkflowDefinitionSnapshot? _other;

                    async Task Because()
                    {
                        _collect = await new CollectWorkflow().GetActive(new WorkflowKey(new WorkflowScope("Default"), new WorkflowId("Collect")));
                        _nudge = await new NudgeWorkflow().GetActive(new WorkflowKey(new WorkflowScope("Default"), new WorkflowId("Nudge")));
                        _other = await new NudgeWorkflow().GetActive(new WorkflowKey(new WorkflowScope("Default"), new WorkflowId("Collect")));
                    }

                    [Fact] void should_hold_a_structurally_valid_collect_graph() => WorkflowGraphStructure.Check(_collect!.Graph).ShouldBeEmpty();
                    [Fact] void should_hold_a_structurally_valid_nudge_graph() => WorkflowGraphStructure.Check(_nudge!.Graph).ShouldBeEmpty();
                    [Fact] void should_not_serve_a_workflow_it_does_not_own() => Assert.Null(_other);
                    [Fact] void should_render_a_state_per_declared_state() => typeof(InvoiceLifecycle).GetNestedTypes().Select(type => type.Name).ShouldContainOnly("Draft", "Sent", "Overdue", "Paid");
                }
                #endif
                """);
            await VerifyGeneratedApplication();
        }
    }
}
#endif
