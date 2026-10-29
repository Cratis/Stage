// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_resolving_symbols;

public class and_two_references_share_a_short_name : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanWith(new() { ["templates/command.sbn"] = "{{ sym.ref 'A' 'Thing' }}{{ sym.ref 'B' 'Thing' }}" });

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_language_refusal() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([given.a_test_language.AmbiguousName]);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
}
