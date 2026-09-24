// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Defines the ordered renderers for admitted semantic slices.
/// </summary>
internal static class SemanticSliceArtifactRenderers
{
    /// <summary>
    /// Gets the renderers in precedence order.
    /// </summary>
    public static IReadOnlyList<ISemanticSliceArtifactRenderer> Ordered { get; } =
    [
        new StateChange(),
        new StateView()
    ];

    static IEnumerable<RenderedFile> RenderWithSpecifications(
        RenderedFile slice,
        LocatedSemanticSlice located,
        SemanticApplicationContext context)
    {
        yield return slice;
        foreach (var specification in located.Slice.Specifications)
        {
            foreach (var file in SemanticSpecificationArtifactRenderer.Render(specification, context))
            {
                yield return file;
            }
        }
    }

    sealed class StateChange : ISemanticSliceArtifactRenderer
    {
        public bool Handles(LocatedSemanticSlice located) => located.Slice.Kind == SemanticSliceKind.StateChange;

        public IEnumerable<RenderedFile> Render(LocatedSemanticSlice located, SemanticApplicationContext context) =>
            RenderWithSpecifications(SemanticStateChangeArtifactRenderer.Render(located, context), located, context);
    }

    sealed class StateView : ISemanticSliceArtifactRenderer
    {
        public bool Handles(LocatedSemanticSlice located) => located.Slice.Kind == SemanticSliceKind.StateView;

        public IEnumerable<RenderedFile> Render(LocatedSemanticSlice located, SemanticApplicationContext context) =>
            RenderWithSpecifications(SemanticStateViewArtifactRenderer.Render(located, context), located, context);
    }
}
