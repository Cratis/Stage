// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_keyed_root_joins : a_generated_application
{
    string _output = string.Empty;

    protected override bool DeferPlan => true;

    async Task Because()
    {
        RenderApplication(CreatePlan());
        await Run("dotnet", "build", "Projects.csproj", "-c", "Debug", "-warnaserror");
        _output = await Run("dotnet", "test", "Projects.csproj", "--no-build", "-c", "Debug");
    }

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(
        when_rendering_a_pure_reducer.Load(when_rendering_keyed_root_joins.Source).GetAwaiter().GetResult());

    [Fact]
    void should_compile_and_update_only_the_instance_named_by_the_event_property_key() =>
        Assert.Contains("Passed!", _output, StringComparison.Ordinal);
}
#endif
