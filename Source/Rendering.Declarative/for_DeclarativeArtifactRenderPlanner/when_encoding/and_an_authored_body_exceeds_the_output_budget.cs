// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_an_authored_body_exceeds_the_output_budget : given.a_body_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var body = new string('a', (int)TemplateBudget.Default.MaxOutputBytes + 1);
        _plan = PlanBody(new() { ["templates/index.sbn"] = "{{ impl.body 'r1' }}" }, Requirement("r1", body), body);
    }

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_output_budget_refusal() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([TemplateCodes.Output]);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
}
