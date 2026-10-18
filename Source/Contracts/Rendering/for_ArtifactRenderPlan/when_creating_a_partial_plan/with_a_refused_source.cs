// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_creating_a_partial_plan;

public class with_a_refused_source : given.an_artifact_render_request
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => ArtifactRenderPlan.Create(
        _request,
        [PlannedArtifact.CreateText("spec.cs", "refused", [_request.Model.Application.Id])],
        [],
        [],
        [new(_request.Model.Application.Id, "specification", ["STAGE-ESM-011"])]));

    [Fact] void should_reject_a_plan_that_still_realizes_the_refusal() => _error.ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
