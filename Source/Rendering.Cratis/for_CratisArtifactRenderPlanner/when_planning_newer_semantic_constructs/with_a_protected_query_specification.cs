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

    [Fact] void should_record_partial_admission() => rendering_refusals.Partial(_plan);
    [Fact] void should_report_specification_admission() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-011").ShouldBeTrue();
    [Fact] void should_omit_the_refused_parts_and_compile_the_rest() => rendering_refusals.OmittedParts(_plan);
}
