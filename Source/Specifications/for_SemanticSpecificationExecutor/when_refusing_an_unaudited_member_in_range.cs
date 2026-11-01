// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_an_unaudited_member_in_range : a_plan_at_an_esm_pair
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRunReport _audited = null!;
    SemanticExecutionPlan _plan32 = null!;

    async Task Because()
    {
        _plan32 = WithApplication(application => application with { Policies = [new("Sensitive policy", new an_unknown_executor_security_condition())] }, 32, 1);
        _report = await new SemanticSpecificationExecutor().Run(_plan32, new([_specification.Id]), new());
        var auditedPlan = WithApplication(application => application with { Policies = [new("Sensitive policy", new an_unknown_executor_security_condition())] }, 32, 0);
        _audited = await new SemanticSpecificationExecutor().Run(auditedPlan, new([_specification.Id]), new());
    }

    [Fact] void should_refuse_the_specification() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_the_stage_version_gate() => _report.Results.Single().Unsupported.Details.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_member_without_its_value() => _report.Results.Single().Unsupported.Details.Contains("an_unknown_executor_security_condition.$type", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_leak_the_declaration_name() => _report.Results.Single().Unsupported.Details.Contains("Sensitive policy", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_refuse_for_the_application() => _report.Results.Single().Unsupported.Construct.ShouldEqual(_plan32.Model.Application.Id.ToString());
    [Fact] void should_not_apply_the_member_check_to_an_audited_pair() => (_audited.Results.Single().Unsupported?.Details.Contains("Unaudited ESM member", StringComparison.Ordinal) == true).ShouldBeFalse();
}
