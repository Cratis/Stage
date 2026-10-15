// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_refusing_a_body_outside_normalized_text : given.a_gap
{
    Exception _failure = null!;
    void Because() => _failure = Catch.Exception(() => ArtifactRenderPlan.Create(
        _request,
        [PlannedArtifact.CreateText(_stub.RelativePath, "\r\nx")],
        [],
        [_gap with { Stubs = [_stub with { BodyLocator = new(0, 3) }] }]));

    [Fact] void should_refuse_a_locator_that_only_fits_before_lf_normalization() => _failure.ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
