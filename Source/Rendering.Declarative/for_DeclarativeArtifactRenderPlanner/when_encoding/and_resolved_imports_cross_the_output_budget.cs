// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_encoding;

public class and_resolved_imports_cross_the_output_budget : given.a_body_planner
{
    static readonly TemplateBudget _budget = TemplateBudget.Default with { MaxOutputBytes = 2000 };
    const string Reference = "{{ sym.ref 'Some.Long.Namespace.Name' 'Thing' }}";

    ArtifactRenderPlan _small = null!;
    ArtifactRenderPlan _large = null!;

    void Because()
    {
        _small = PlanBody(new() { ["templates/index.sbn"] = Reference + string.Concat(Enumerable.Repeat("{{ sym.imports }}", 10)) }, null, null, _budget);
        _large = PlanBody(new() { ["templates/index.sbn"] = Reference + string.Concat(Enumerable.Repeat("{{ sym.imports }}", 200)) }, null, null, _budget);
    }

    [Fact] void should_render_the_expansion_within_the_budget() => _small.Success.ShouldBeTrue();
    [Fact] void should_have_no_artifacts_when_the_expansion_crosses_it() => _large.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_output_budget_refusal() => _large.Diagnostics.Select(_ => _.Code).ShouldEqual([TemplateCodes.Output]);
}
