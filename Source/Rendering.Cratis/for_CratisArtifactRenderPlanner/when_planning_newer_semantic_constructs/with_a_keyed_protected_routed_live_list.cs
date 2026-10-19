// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_keyed_protected_routed_live_list : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(a_protected_collection_model.KeyedRouted));

    [Fact] void should_admit_the_observable_pinned_rows() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_the_collection_assertion() => _plan.Artifacts.Any(artifact => System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("should_return_the_expected_count", StringComparison.Ordinal)).ShouldBeTrue();
}
