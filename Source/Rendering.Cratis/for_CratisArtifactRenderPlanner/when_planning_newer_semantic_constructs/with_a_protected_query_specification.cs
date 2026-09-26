// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_protected_query_specification : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = "policy Clerks\n  require authenticated\n" + when_rendering_scoped_projections.ScopedSource
            .Replace("query ProjectById => ProjectSummary?\n", "query ProjectById => ProjectSummary?\n                authorize Clerks\n", StringComparison.Ordinal)
            .Replace("specification RegisteringAProject\n", "specification RegisteringAProject\n                given caller\n                  authenticated\n", StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_admit_the_protected_query() => _plan.Success.ShouldBeTrue();
    [Fact] void should_use_arcs_query_pipeline() => _plan.Artifacts.Select(artifact => System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).Any(code => code.Contains("_scenario.Perform(nameof(ProjectSummary.ProjectById)", StringComparison.Ordinal)).ShouldBeTrue();
}
