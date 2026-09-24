// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_stream_different_from_the_produced_destination : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = when_rendering_scoped_projections.ScopedSource.Replace(
            "        then ProjectRegistered\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
            "        then ProjectRegistered\n          for \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
            StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_reject_an_unreproducible_event_stream() => _plan.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-011");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
