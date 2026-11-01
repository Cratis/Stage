// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// The model-bound rendering of running totals and clears builds without warnings and passes the modeled specifications
/// that the fluent rendering of the same source passes.
/// </summary>
public class when_executing_model_bound_running_totals : a_generated_application
{
    ArtifactRenderPlan _plan = null!;

    protected override ArtifactRenderPlan CreatePlan() =>
        _plan = when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(when_rendering_model_bound_running_totals.Source).GetAwaiter().GetResult(), self: true);

    [Fact]
    async Task should_run_the_projected_specifications()
    {
        try
        {
            var projection = System.Text.Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Shelf/Shelf.cs", StringComparison.Ordinal)).Bytes.AsSpan());
            Assert.DoesNotContain("#pragma", projection, StringComparison.Ordinal);
            Assert.DoesNotContain("IProjectionFor", projection, StringComparison.Ordinal);
            var build = await Run("model-bound-totals-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("model-bound-totals-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", results, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
