// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_expanding_outputs;

public class and_fan_out_exceeds_the_budget : given.a_declarative_planner
{
    Contracts.Rendering.ArtifactRenderPlan _plan = null!;

    void Because() => _plan = new DeclarativeArtifactRenderPlanner([new given.a_test_language()], new(TemplateBudget.Default with { MaxFanOut = 1 })).Plan(Request(Profile(Archive())));

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_fan_out_refusal() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([TemplateCodes.FanOut]);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
}
