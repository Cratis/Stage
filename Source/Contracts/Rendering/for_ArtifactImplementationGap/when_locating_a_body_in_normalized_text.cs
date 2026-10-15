// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_locating_a_body_in_normalized_text : given.a_gap
{
    ArtifactRenderPlan _plan = null!;
    void Because() => _plan = ArtifactRenderPlan.Create(
        _request,
        [PlannedArtifact.CreateText(_stub.RelativePath, "\r\n\U0001F600")],
        [],
        [_gap with { Stubs = [_stub with { BodyLocator = new(1, 2) }] }]);

    [Fact] void should_allow_a_body_ending_at_the_normalized_utf16_length() => _plan.ImplementationGaps.Length.ShouldEqual(1);
}
