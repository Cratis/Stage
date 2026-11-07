// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.for_GeneratedTypeNames;

public class when_inventorying_cross_slice_projection_targets : Specification
{
    [Theory]
    [InlineData("WelcomeRows", "WelcomeRows")]
    [InlineData("WelcomeRow", "WelcomeRowProjection")]
    public async Task should_match_the_renderer_using_the_target_from_the_application(string projectionName, string expectedName)
    {
        var source = builderless_state_view_model.BuiltQuery
            .Replace("        id Uuid\n        text String\n      query", "        id Uuid key\n        text String\n      query", StringComparison.Ordinal)
            .Replace("slice StateView WelcomeInfo\n      readmodel WelcomeRow", "slice StateView Rows\n      readmodel WelcomeRow\n        id Uuid key\n        text String\n    slice StateView WelcomeInfo\n      readmodel Other", StringComparison.Ordinal)
            .Replace("projection WelcomeRows =>", $"projection {projectionName} =>", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var application = loaded.Model.Application;
        var options = new CratisRenderingOptions("Projects", "Projects");
        var request = new ArtifactRenderRequest(loaded.Model, loaded.Plan, CratisRendering.CreateProfile("Projects", options), new(ArtifactRenderScopeKind.Application, application.Id));
        var context = new SemanticApplicationContext(request, options);
        context.ScopeFlatProjections();
        var selected = context.SelectedSlices().Single(located => located.Slice.Name == "WelcomeInfo");
        var projection = Assert.Single(selected.Slice.Projections);
        var declaration = Assert.Single(GeneratedTypeNames.Declarations(application, [(selected.Path, selected.Slice)]), declaration => declaration.Kind == "Projection");
        Assert.Equal(expectedName, declaration.Name);
        var rendered = SemanticScopedProjectionRenderer.Render(projection, context.ReadModels[projection.ReadModel], projection.Scope, context);
        Assert.Contains($"public class {declaration.Name} :", rendered, StringComparison.Ordinal);
    }
}
#endif
