// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_creating_a_partial_plan;

public class with_malformed_refusals : given.an_artifact_render_request
{
    [Fact] void should_reject_uninitialized_metadata() => Catch.Exception(() => ArtifactRenderPlan.Create(_request, [], [], [], default)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_reject_an_unset_identity() => Reject(new(default, "specification", ["STAGE-ESM-011"]));
    [Fact] void should_reject_a_missing_kind() => Reject(new(_request.Model.Application.Id, " ", ["STAGE-ESM-011"]));
    [Fact] void should_reject_missing_codes() => Reject(new(_request.Model.Application.Id, "specification", []));
    [Fact] void should_reject_a_blank_code() => Reject(new(_request.Model.Application.Id, "specification", [" "]));
    [Fact] void should_reject_null_metadata() => Reject(null!);
    [Fact] void should_reject_duplicate_identities() => Catch.Exception(() => ArtifactRenderPlan.Create(
        _request,
        [],
        [],
        [],
        [new(_request.Model.Application.Id, "specification", ["STAGE-ESM-011"]), new(_request.Model.Application.Id, "specification", ["STAGE-ESM-024"])])).ShouldBeOfExactType<InvalidArtifactRenderContract>();

    void Reject(ArtifactRenderRefusal refusal) => Catch.Exception(() => ArtifactRenderPlan.Create(_request, [], [], [], [refusal])).ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
