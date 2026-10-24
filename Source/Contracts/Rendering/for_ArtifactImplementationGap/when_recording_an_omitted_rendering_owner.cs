// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_recording_an_omitted_rendering_owner : given.a_gap
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = ArtifactRenderPlan.Create(
        _request,
        [],
        [],
        [_gap with { Kind = ArtifactImplementationGapKind.Rendering, Stubs = [] }]);

    [Fact] void should_record_the_owner_without_a_payload_stub() => _plan.ImplementationGaps.Single().Owner.ShouldEqual(_gap.Owner);
    [Fact] void should_mark_the_plan_partial() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_include_the_gap_kind_in_the_digest() => Digest([_gap with { Kind = ArtifactImplementationGapKind.Rendering }]).ShouldNotEqual(Digest([_gap]));
    [Fact] void should_preserve_the_default_payload_gap_digest() => Digest([_gap with { Kind = ArtifactImplementationGapKind.Implementation }]).ShouldEqual(Digest([_gap]));
}
