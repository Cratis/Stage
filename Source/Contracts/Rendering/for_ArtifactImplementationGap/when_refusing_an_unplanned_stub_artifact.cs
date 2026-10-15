// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_refusing_an_unplanned_stub_artifact : given.a_gap
{
    Exception _failure = null!;
    void Because() => _failure = Catch.Exception(() => ArtifactRenderPlan.Create(_request, [], [], [_gap]));

    [Fact] void should_refuse_a_stub_whose_path_is_not_in_the_plan() => _failure.ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
