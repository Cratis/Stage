// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// The fluent rendering of the model-bound running totals source: the reference the model-bound twin must match.
/// </summary>
public class when_executing_fluent_running_totals : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan() =>
        when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(when_rendering_model_bound_running_totals.Source).GetAwaiter().GetResult(), self: false);

    [Fact]
    async Task should_run_the_projected_specifications()
    {
        try
        {
            var build = await Run("fluent-totals-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("fluent-totals-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", results, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
