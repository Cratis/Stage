// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_admitting_the_profile;

public class and_the_target_does_not_match_the_manifest : a_refused_plan
{
    protected override string ExpectedCode => DeclarativeCodes.TargetMismatch;

    void Because() => _plan = Plan(Profile(Archive(), target: "other"));

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_only_the_expected_error() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([ExpectedCode]);
}
