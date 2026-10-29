// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_repeated_substitutions_cross_the_output_budget : given.a_body_planner
{
    static readonly string _body = new('a', 400);
    static readonly TemplateBudget _budget = TemplateBudget.Default with { MaxOutputBytes = 1000 };

    ArtifactRenderPlan _two = null!;
    ArtifactRenderPlan _three = null!;

    void Because()
    {
        _two = PlanBody(new() { ["templates/index.sbn"] = "{{ impl.body 'r1' }}{{ impl.body 'r1' }}" }, Requirement("r1", _body), _body, _budget);
        _three = PlanBody(new() { ["templates/index.sbn"] = "{{ impl.body 'r1' }}{{ impl.body 'r1' }}{{ impl.body 'r1' }}" }, Requirement("r1", _body), _body, _budget);
    }

    [Fact] void should_render_two_substitutions_within_the_budget() => _two.Success.ShouldBeTrue();
    [Fact] void should_refuse_the_third_with_no_artifacts() => _three.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_output_budget_refusal() => _three.Diagnostics.Select(_ => _.Code).ShouldEqual([TemplateCodes.Output]);
}
