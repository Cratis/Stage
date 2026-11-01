// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_admitting_the_profile;

public class and_two_languages_share_the_pack : a_refused_plan
{
    protected override string ExpectedCode => DeclarativeCodes.LanguageNotBound;

    void Because() => _plan = Plan(Profile(Archive()), new given.a_test_language(), new given.a_test_language());

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_only_the_expected_error() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([ExpectedCode]);
}
