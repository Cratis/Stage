// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_validating_paths;

public class and_paths_collide_by_case : a_refused_path_plan
{
    protected override string ExpectedCode => DeclarativeCodes.PathCollision;

    void Because() => _plan = PlanPaths("Index.test", "index.test");

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_only_the_expected_error() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([ExpectedCode]);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
}
