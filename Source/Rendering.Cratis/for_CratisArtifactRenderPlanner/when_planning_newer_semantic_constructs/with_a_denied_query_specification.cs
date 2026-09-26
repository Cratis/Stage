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
            .Replace("specification RegisteringAProject\n", "specification RegisteringAProject\n        given caller\n          authenticated\n", StringComparison.Ordinal)
            .Replace("specification LookingUpPinnedProject\n",
                "specification DenyingAnonymousProjectLookup\n        given caller\n          role \"Guest\"\n        then query ProjectById\n          arguments\n            projectId = \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n        then denied\n      specification LookingUpPinnedProject\n",
                StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_admit_the_denied_query() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_assert_both_unauthorized_and_absent_data() => _plan.Artifacts.Select(artifact => System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).Any(code => code.Contains("_result.IsAuthorized.ShouldBeFalse()", StringComparison.Ordinal) && code.Contains("_result.Data.ShouldBeNull()", StringComparison.Ordinal)).ShouldBeTrue();
}
