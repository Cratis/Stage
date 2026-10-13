// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_release_baseline;

public class with_a_loaded_assessment : a_release_baseline_request
{
    CratisPlanResult _result = null!;
    void Because() => _result = CratisRendering.PlanFrom(new LoadedSemanticModel(_model, _executionPlan) { ReleaseBaseline = _assessment }, new([PlanSelectionEntry.Slice(_module.Name, _feature.Name, _projectLookup.Name)]), new(_model.Application.Name, _options.ProjectName, _options.RootNamespace));
    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_forward_the_whole_application_assessment() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("STAGE-BASELINE-001", "STAGE-BASELINE-002");
    [Fact] void should_include_warnings_on_the_underlying_plan() => _result.Plan!.Diagnostics.Length.ShouldEqual(2);
}
#endif
