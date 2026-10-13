// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_release_baseline_warnings : a_release_baseline_request
{
    CratisPublicationCheck _check = null!;
    void Establish() => _plan = _planner.Plan(WithBaseline(_assessment));
    void Because() => _check = CratisRendering.CheckPublication(_plan, path => _plan.Artifacts.FirstOrDefault(artifact => artifact.RelativePath == path) is { } artifact ? Text(artifact) : null);
    [Fact] void should_remain_compatible() => _check.ShouldBeOfExactType<CratisPublicationCheck.Compatible>();
    [Fact] void should_have_assessed_baseline_violations() => _plan.Diagnostics.Length.ShouldEqual(2);
}
#endif
