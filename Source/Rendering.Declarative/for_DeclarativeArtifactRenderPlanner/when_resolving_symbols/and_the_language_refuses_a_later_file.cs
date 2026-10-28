// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_resolving_symbols;

public class and_the_language_refuses_a_later_file : given.a_recording_language_planner
{
    readonly given.a_test_language _language = new();
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        _language.RefusedPaths["Reactions/EchoReaction.test"] = given.a_test_language.AmbiguousName;
        _plan = PlanWithLanguage([], _language);
    }

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_language_refusal() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([given.a_test_language.AmbiguousName]);
}
