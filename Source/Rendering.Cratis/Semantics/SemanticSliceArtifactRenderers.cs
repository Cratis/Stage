// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

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
        new StateView(),
        new Automation(),
        new Translate()
    ];

    static IEnumerable<RenderedFile> RenderWithSpecifications(
        RenderedFile slice,
        LocatedSemanticSlice located,
        SemanticApplicationContext context)
    {
        yield return slice;
        foreach (var specification in located.Slice.Specifications.Where(specification => !context.RefusedSpecifications.Contains(specification.Id)))
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

    sealed class Automation : ISemanticSliceArtifactRenderer
    {
        public bool Handles(LocatedSemanticSlice located) => located.Slice.Kind == SemanticSliceKind.Automation;

        public IEnumerable<RenderedFile> Render(LocatedSemanticSlice located, SemanticApplicationContext context)
        {
            foreach (var file in RenderWithSpecifications(SemanticAutomationArtifactRenderer.Render(located, context), located, context))
            {
                yield return file;
            }
            foreach (var reducer in located.Slice.Reducers.Where(reducer => !context.SelfReduces(reducer.ReadModel)))
            {
                yield return SemanticReducerArtifactRenderer.Render(located, reducer, context);
            }
            foreach (var reaction in located.Slice.Reactions)
            {
                yield return SemanticReactorArtifactRenderer.Render(located, reaction, context);
            }
        }
    }

    sealed class Translate : ISemanticSliceArtifactRenderer
    {
        public bool Handles(LocatedSemanticSlice located) => located.Slice.Kind == SemanticSliceKind.Translate;

        public IEnumerable<RenderedFile> Render(LocatedSemanticSlice located, SemanticApplicationContext context)
        {
            if (!located.Slice.Events.IsEmpty || !located.Slice.Constraints.IsDefaultOrEmpty)
            {
                yield return SemanticStateChangeArtifactRenderer.Render(located, context) with
                {
                    RelativePath = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{Identifiers.ToPascalCase(located.Slice.Name)}.Events.cs"])
                };
            }
            foreach (var capture in located.Slice.Captures)
            {
                yield return SemanticCaptureArtifactRenderer.Render(located, capture, context);
            }
            foreach (var projection in located.Slice.Projections)
            {
                yield return SemanticPublicationArtifactRenderer.Render(located, projection, context);
            }
        }
    }

    sealed class StateView : ISemanticSliceArtifactRenderer
    {
        public bool Handles(LocatedSemanticSlice located) => located.Slice.Kind == SemanticSliceKind.StateView;

        public IEnumerable<RenderedFile> Render(LocatedSemanticSlice located, SemanticApplicationContext context)
        {
            foreach (var file in RenderWithSpecifications(SemanticStateViewArtifactRenderer.Render(located, context), located, context))
            {
                yield return file;
            }

            foreach (var reducer in located.Slice.Reducers.Where(reducer => !context.SelfReduces(reducer.ReadModel)))
            {
                yield return SemanticReducerArtifactRenderer.Render(located, reducer, context);
            }
        }
    }
}
