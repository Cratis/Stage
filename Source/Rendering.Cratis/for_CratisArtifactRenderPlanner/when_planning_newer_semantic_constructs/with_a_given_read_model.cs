// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_given_read_model : Specification
{
    string[] _errors = null!;
    string[] _reasons = null!;
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = register_project_scenarios.WithPriorProjectAndSubsetExpectation.Replace(
            "specification RegisteringAProject\n",
            "specification RegisteringAProject\n        given readmodel ProjectSummary\n          projectId = \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n          name = \"Pinned state\"\n",
            StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
        _errors = [.. _plan.Diagnostics.Select(_ => _.Code)];
        _reasons = [.. _plan.Diagnostics.Select(_ => _.Message)];
    }

    [Fact] void should_reject_unreproducible_pinned_state() => _errors.ShouldContain("STAGE-ESM-011");
    [Fact] void should_omit_the_refused_parts_and_compile_the_rest() => rendering_refusals.OmittedParts(_plan);
    [Fact] void should_explain_why_seeded_read_model_state_is_not_equivalent() => _reasons.Any(_ => _.Contains("cannot initialize each projected instance", StringComparison.Ordinal)).ShouldBeTrue();
}
