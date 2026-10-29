// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_inventorying_reducer_typed_contexts;

public class with_min_a3_generated_application : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan() =>
        new CratisArtifactRenderPlanner().Plan(reducer_context_model.Request(reducer_context_model.LoadMinA3().GetAwaiter().GetResult()));

    [Fact]
    public async Task should_compile_the_unchanged_min_a3_generated_application()
    {
        var output = await Run("min-a3-build.log", "build", "MinA3.csproj", "-c", "Debug", "-warnaserror");
        Assert.Contains("Build succeeded.", output, StringComparison.Ordinal);
        Assert.DoesNotContain(GeneratedFiles("TypedContext_*.cs"), path => path.Contains("_StateShape", StringComparison.Ordinal) || path.Contains("_EventShape", StringComparison.Ordinal));
        Assert.NotEmpty(GeneratedFiles("TypedContext_*.cs"));
    }
}
#endif
