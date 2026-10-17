// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_creating_a_partial_plan;

public class with_unordered_refusals : given.an_artifact_render_request
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _reordered = null!;
    readonly SemanticId _first = SemanticId.Parse($"sem1:{new string('0', 64)}");
    readonly SemanticId _last = SemanticId.Parse($"sem1:{new string('f', 64)}");

    void Because()
    {
        _plan = ArtifactRenderPlan.Create(
            _request,
            [],
            [],
            [],
            [new(_last, "specification", ["STAGE-ESM-024", "STAGE-ESM-011"]), new(_first, "specification", ["STAGE-ESM-011"])]);
        _reordered = ArtifactRenderPlan.Create(
            _request,
            [],
            [],
            [],
            [new(_first, "specification", ["STAGE-ESM-011"]), new(_last, "specification", ["STAGE-ESM-011", "STAGE-ESM-024"])]);
    }

    [Fact] void should_order_identities_ordinally() => _plan.Refusals.Select(refusal => refusal.Artifact).SequenceEqual([_first, _last]).ShouldBeTrue();
    [Fact] void should_hash_normalized_metadata() => _plan.Digest.ShouldEqual(_reordered.Digest);
}
