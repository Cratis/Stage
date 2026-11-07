// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticLifecycleSpecifications;

public class when_running_the_lifecycle_specifications : Specification
{
    SemanticSpecificationRunReport _all = null!;
    SemanticSpecificationRunReport _scoped = null!;
    SemanticSpecificationRunReport _renderable = null!;
    SemanticExecutionPlan _plan = null!;

    async Task Establish() => _plan = a_lifecycle_source.Plan();

    async Task Because()
    {
        var executor = new SemanticSpecificationExecutor();
        _all = await executor.Run(_plan, SemanticSpecificationSelection.All, new());
        _scoped = await executor.Run(_plan, new([_plan.Model.Application.Modules[0].Id]), new());
        _renderable = await executor.Run(a_lifecycle_source.Plan(a_lifecycle_source.RenderableSource), SemanticSpecificationSelection.All, new());
    }

    [Fact]
    void should_run_every_state_machine_and_workflow_specification() => _all.Results.Where(result => result.SliceKind is "StateMachine" or "Workflow").Select(result => result.Name).ShouldContainOnly(
        ["SendingStartsTheCollection", "AnOverdueInvoiceIsOverdue", "APaidInvoiceIsTagged", "NoPaymentIsWrittenOff", "TheRunWaits"]);
    [Fact] void should_report_nothing_failing() => _all.Results.SelectMany(result => result.Failures).ShouldBeEmpty();
    [Fact] void should_pass_them() => _all.Results.Where(result => result.SliceKind is "StateMachine" or "Workflow").All(result => result.Outcome == SemanticSpecificationOutcome.Passed).ShouldBeTrue();
    [Fact] void should_pass_the_renderable_lifecycle_too() => _renderable.Results.Where(result => result.SliceKind is "StateMachine" or "Workflow").Select(result => string.Concat(result.Name, ": ", string.Join(";", result.Failures))).ShouldContainOnly(["SendingStartsTheCollection: ", "AnOverdueInvoiceIsOverdue: ", "APaidInvoiceIsTagged: ", "NoPaymentIsWrittenOff: ", "AnOverdueInvoiceIsTagged: "]);
    [Fact] void should_leave_them_out_of_a_module_scope() => _scoped.Results.Any(result => result.SliceKind is "StateMachine" or "Workflow").ShouldBeFalse();
}
