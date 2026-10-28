// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_resolving_symbols;

public class and_the_language_returns_the_wrong_count : given.a_recording_language_planner
{
    readonly given.a_test_language _language = new() { DropOneResolution = true };
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanWithLanguage([], _language);

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_language_contract_refusal() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([DeclarativeCodes.LanguageContract]);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
}
