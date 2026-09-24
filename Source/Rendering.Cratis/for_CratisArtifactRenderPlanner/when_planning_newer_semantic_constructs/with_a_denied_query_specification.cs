// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_denied_query_specification : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = "policy Clerks\n  require authenticated\n" + when_rendering_scoped_projections.ScopedSource
            .Replace("query ProjectById => ProjectSummary?\n", "query ProjectById => ProjectSummary?\n                authorize Clerks\n", StringComparison.Ordinal)
            .Replace("specification LookingUpPinnedProject\n",
                "specification DenyingAnonymousProjectLookup\n        given caller\n          role \"Guest\"\n        then query ProjectById\n          arguments\n            projectId = \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n        then denied\n      specification LookingUpPinnedProject\n",
                StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_reject_a_query_denial_without_pipeline_execution() => _plan.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-011");
    [Fact] void should_name_the_missing_unauthorized_outcome_check() => _plan.Diagnostics.Any(_ => _.Message.Contains("Unauthorized and no returned data", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_emit_no_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
