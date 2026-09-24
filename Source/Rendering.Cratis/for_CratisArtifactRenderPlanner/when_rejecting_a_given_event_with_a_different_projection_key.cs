// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_a_given_event_with_a_different_projection_key : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = register_project_scenarios.WithPriorProjectAndSubsetExpectation.Replace(
            "projectId = \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"",
            "projectId = \"5fa85f64-5717-4562-b3fc-2c963f66afa8\"",
            StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_refuse_the_specification() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-011");
    [Fact] void should_emit_no_partial_application() => _plan.Artifacts.ShouldBeEmpty();
}
