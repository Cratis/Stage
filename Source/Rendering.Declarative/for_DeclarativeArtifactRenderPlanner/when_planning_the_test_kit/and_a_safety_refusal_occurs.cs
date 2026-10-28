// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_planning_the_test_kit;

public class and_a_safety_refusal_occurs : given.a_declarative_planner
{
    List<(string Name, ArtifactRenderPlan Plan)> _plans = null!;

    void Because() => _plans =
    [
        ("renderer", Plan(Profile(Archive(), renderer: "other"))),
        ("target", Plan(Profile(Archive(), target: "other"))),
        ("template", PlanWith(new() { ["templates/reaction.sbn"] = "{{ refuse 'trust' 'unsafe' }}" })),
        ("sentinel", PlanWith(new() { ["templates/index.sbn"] = "/*__stage_" })),
        ("path", Plan(Profile(Archive(KitFiles(Manifest.Replace("Index.test", "../Index.test", StringComparison.Ordinal)))))),
        ("collision", Plan(Profile(Archive(KitFiles(Manifest.Replace("Index.test", "Commands/echo.test", StringComparison.Ordinal))))))
    ];

    [Fact] void should_render_no_artifact_for_any_of_them() => _plans.Where(_ => !_.Plan.Artifacts.IsEmpty).Select(_ => _.Name).ShouldBeEmpty();
    [Fact] void should_not_succeed_for_any_of_them() => _plans.Where(_ => _.Plan.Success).Select(_ => _.Name).ShouldBeEmpty();
    [Fact] void should_report_an_error_for_each_of_them() => _plans.Where(_ => !_.Plan.Diagnostics.Any(d => d.Severity == ArtifactRenderDiagnosticSeverity.Error)).Select(_ => _.Name).ShouldBeEmpty();
}
