// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_refusing_malformed_locators : given.a_gap
{
    Exception _negative = null!;
    Exception _empty = null!;
    Exception _overflowing = null!;
    void Because()
    {
        _negative = Refuse(new(-1, 1));
        _empty = Refuse(new(1, 0));
        _overflowing = Refuse(new(int.MaxValue, 1));
    }

    [Fact] void should_refuse_negative_starts() => _negative.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_empty_bodies() => _empty.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_overflowing_ends() => _overflowing.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    Exception Refuse(ArtifactImplementationBodyLocator locator)
    {
        var exception = Catch.Exception(() => Digest([_gap with { Stubs = [_stub with { BodyLocator = locator }] }]));
        exception.ShouldNotBeNull();
        return exception;
    }
}
