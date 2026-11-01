// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_creating_a_gap_for_a_live_performer : given.a_gap
{
    ArtifactImplementationGap _live = null!;
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        _live = _gap with { Origin = ArtifactImplementationGapOrigin.Unverified, ResultVersion = 2 };
        _plan = ArtifactRenderPlan.Create(_request, _artifacts, [], [_live]);
    }

    [Fact] void should_accept_the_live_contract() => _plan.ImplementationGaps[0].ResultVersion.ShouldEqual(2u);
    [Fact] void should_preserve_the_context_version() => _plan.ImplementationGaps[0].ContextVersion.ShouldEqual(1u);
    [Fact] void should_include_the_result_version_in_the_digest() => Digest([_live]).ShouldNotEqual(Digest([_live with { ResultVersion = 1 }]));
    [Fact] void should_include_the_context_version_in_the_digest() => Digest([_live]).ShouldNotEqual(Digest([_live with { ContextVersion = 2 }]));
}
