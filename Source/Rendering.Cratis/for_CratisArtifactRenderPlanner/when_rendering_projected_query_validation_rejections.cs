// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_projected_query_validation_rejections : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(a_projected_query_validation_model.Compile());

    [Fact] void should_admit_the_projected_view() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_emit_the_projection() => _plan.Artifacts.Any(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("[FromEvent<", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_emit_all_four_rejection_scenarios() => _plan.Artifacts.Count(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("void should_reject_the_query()", StringComparison.Ordinal)).ShouldEqual(4);
    [Fact] void should_emit_both_success_scenarios() => _plan.Artifacts.Count(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("void should_accept_the_query()", StringComparison.Ordinal)).ShouldEqual(2);
}
