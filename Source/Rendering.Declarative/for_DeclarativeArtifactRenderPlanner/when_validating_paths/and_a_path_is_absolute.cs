// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_validating_paths;

public class and_a_path_is_absolute : a_refused_path_plan
{
    protected override string ExpectedCode => DeclarativeCodes.PathInvalid;

    void Because() => _plan = PlanPaths("/etc/Absolute.test");

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_only_the_expected_error() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([ExpectedCode]);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
}
