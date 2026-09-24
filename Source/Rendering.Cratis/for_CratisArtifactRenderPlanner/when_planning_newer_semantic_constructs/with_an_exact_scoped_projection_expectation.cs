// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_an_exact_scoped_projection_expectation : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = when_rendering_scoped_projections.ScopedSource.Replace(
            "then readmodel ProjectSummary\n", "then readmodel ProjectSummary exactly\n", StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_reject_unobservable_absent_properties() => _plan.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-011");
    [Fact] void should_explain_the_exact_comparison_gap() => _plan.Diagnostics.Any(_ => _.Message.Contains("unset properties", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
