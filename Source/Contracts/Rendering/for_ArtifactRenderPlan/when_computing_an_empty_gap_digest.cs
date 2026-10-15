// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_computing_an_empty_gap_digest : given.an_artifact_render_request
{
    ArtifactRenderPlan _plan = null!;
    void Because() => _plan = ArtifactRenderPlan.Create(_request, [], [], []);

    // Frozen from origin/main 2338d2a6's v1 binary digest fields, before implementation-gap support.
    [Fact] void should_preserve_the_original_digest_bytes() => _plan.Digest.ShouldEqual("a404a5505039c4685af12594bf7d039f80e779e9678b02814c430d976460b419");
    [Fact] void should_match_the_original_create_overload() => _plan.Digest.ShouldEqual(ArtifactRenderPlan.Create(_request, [], []).Digest);
    [Fact] void should_match_the_original_digest_overload() => _plan.Digest.ShouldEqual(ArtifactRenderPlan.ComputeDigest("cratis", "1", "Cratis.Stage.Rendering.Cratis", "1", "Projects", [], []));
    [Fact] void should_have_no_gaps() => _plan.ImplementationGaps.ShouldBeEmpty();
}
