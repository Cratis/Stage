// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_readable_inline_names;

public class with_typed_context_wrappers
{
    [Fact]
    public async Task should_name_the_reducer_wrapper_and_helper_without_hashes()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.Source);
        var context = when_rendering_a_pure_reducer.Context(loaded);
        var descriptor = Assert.Single(loaded.TypedContextDescriptors);
        Assert.EndsWith("Context", context.ContextNames.ContextWrapper(descriptor), StringComparison.Ordinal);
        Assert.DoesNotContain("_", context.ContextNames.ContextWrapper(descriptor), StringComparison.Ordinal);
        Assert.Equal("OrderPlacedTransition", context.InlineNames.ReducerTransition(descriptor.RequirementId));
    }

    [Fact]
    public async Task should_allocate_the_same_names_for_a_scoped_and_a_full_render()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.Source);
        var full = when_rendering_a_pure_reducer.Plan(loaded);
        var scoped = when_rendering_a_pure_reducer.Plan(loaded, new(ArtifactRenderScopeKind.Slice, loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.ReadModels.Length > 0).Id));
        Assert.True(full.Success && scoped.Success);
        var scopedFiles = scoped.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("TypedContexts/", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(scopedFiles);
        Assert.All(scopedFiles, file => Assert.Contains(full.Artifacts, artifact => artifact.RelativePath == file.RelativePath && artifact.Sha256 == file.Sha256));
    }
}
#endif
