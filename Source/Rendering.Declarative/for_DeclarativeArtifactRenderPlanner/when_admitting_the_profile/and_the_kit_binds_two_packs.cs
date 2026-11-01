// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_admitting_the_profile;

public class and_the_kit_binds_two_packs : a_refused_plan
{
    protected override string ExpectedCode => DeclarativeCodes.LanguagePackCount;

    void Because() => _plan = Plan(
        Profile(Archive(KitFiles(Manifest.Replace("\"test@1.0.0\"", "\"test@1.0.0\",\"other@1.0.0\"", StringComparison.Ordinal)))),
        new given.a_test_language(),
        new given.a_test_language("1.0.0", "other"));

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_only_the_expected_error() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([ExpectedCode]);
}
