// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_denied_caller : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun _reference = null!;

    async Task Because()
    {
        var command = _plan.Commands[_specification.When!.Command] with { Authorization = new SemanticPolicyReference("MayRegister") };
        var policy = new SemanticPolicy("MayRegister", new SemanticLogicalPolicyCondition(new SemanticAuthenticatedCondition(), SemanticLogicalOperator.And, new SemanticRoleCondition("Registrar")));
        var specification = _specification with { ThenEvents = [], ThenDenied = true, GivenCaller = new SemanticCaller(true, ["Visitor"], []) };
        var plan = WithBehavior(specification, command, policy: policy, keepProjections: true);
        _reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        _report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new());
    }

    [Fact] void should_report_unauthorized_like_the_reference() => Assert.True(_reference.Passed && _reference.Execution is SemanticRejected { Category: SemanticRejectionCategory.Unauthorized } && _report.Results.Single().Outcome == SemanticSpecificationOutcome.Passed && _report.Results.Single().ExecutionKind == "Rejected", $"Reference: {_reference.Execution.Kind} {string.Join(',', _reference.Failures)}; Stage: {_report.Results.Single().Outcome} {_report.Results.Single().Unsupported?.Details} {string.Join(',', _report.Results.Single().Failures)}");
    [Fact] void should_append_no_facts() => _report.Results.Single().Trace!.Facts.Count.ShouldEqual(0);
}
