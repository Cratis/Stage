// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_creating_screen_partitions : given.a_partitioned_screen_plan
{
    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("unresolved")]
    public void should_refuse_a_scene_that_disagrees_with_the_compilation(string reason)
    {
        var profile = CratisRendering.CreateProfile("Projects", new("Projects", "Projects"), _scene with
        {
            Screens = reason switch
            {
                "missing" => [.. _scene.Screens.Skip(1)],
                "duplicate" => [.. _scene.Screens.Concat(_scene.Screens.Take(1))],
                _ => [.. _scene.Screens.Select(screen => screen with { Name = "Unrelated" })]
            }
        });
        Catch.Exception(() => CratisRendering.WithScreenPartitions(profile, _compilation)).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    }
}
