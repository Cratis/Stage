// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_RefusalModeInput;

public class when_refusing_unknown_inputs : Specification
{
    [Theory]
    [InlineData("cratis-render:refusals", "1", "renderaround")]
    [InlineData("cratis-render:refusals", "1", "RenderAround\n")]
    [InlineData("cratis-render:refusals", "1", "Unknown")]
    [InlineData("cratis-render:refusals", "2", "RenderAround")]
    [InlineData("other", "1", "RenderAround")]
    public void should_reject_noncanonical_inputs(string name, string version, string value)
    {
        RefusalModeInput.TryRead(ArtifactRenderInput.Create(name, version, [.. Encoding.UTF8.GetBytes(value)]), out var mode).ShouldBeFalse();
        mode.ShouldEqual(ArtifactRefusalMode.Refuse);
    }

    [Fact] void should_reject_unknown_modes() => Catch.Exception(() => RefusalModeInput.Create((ArtifactRefusalMode)99)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
