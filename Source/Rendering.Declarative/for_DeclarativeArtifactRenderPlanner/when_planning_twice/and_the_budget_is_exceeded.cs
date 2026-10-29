// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_planning_twice;

public class and_the_budget_is_exceeded : given.a_declarative_planner
{
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _second = null!;

    void Because()
    {
        var planner = new DeclarativeArtifactRenderPlanner([new given.a_test_language()], new(TemplateBudget.Default with { MaxFanOut = 1 }));
        var request = Request(Profile(Archive()));
        _first = planner.Plan(request);
        _second = planner.Plan(request);
    }

    [Fact] void should_have_no_artifacts_either_time() => (_first.Artifacts.Length + _second.Artifacts.Length).ShouldEqual(0);
    [Fact] void should_refuse_identically() => _second.Digest.ShouldEqual(_first.Digest);
}
