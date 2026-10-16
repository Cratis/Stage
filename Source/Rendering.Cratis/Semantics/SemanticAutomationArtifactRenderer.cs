// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Composes the commands, events and read surface declared by an Automation slice.
/// </summary>
internal static class SemanticAutomationArtifactRenderer
{
    internal static RenderedFile Render(LocatedSemanticSlice located, SemanticApplicationContext context)
    {
        var builder = new CSharpCodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        var hasReadSurface = SemanticReadSurface.IsDeclaredBy(located.Slice);
        if (hasReadSurface)
        {
            context.ReducerBindings(located).Configure(builder);
        }
        else
        {
            SemanticStateChangeArtifactRenderer.Configure(builder, located, context);
        }
        SemanticStateChangeArtifactRenderer.RenderMembers(builder, located, context);
        if (hasReadSurface)
        {
            builder.BlankLine();
            SemanticStateViewArtifactRenderer.RenderMembers(builder, located, context);
        }

        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), SliceNaming.FileName(located.Slice.Name)]), builder.ToString())
        {
            Sources = [located.Slice.Id, .. located.Slice.Commands.Select(command => command.Id), .. located.Slice.Events.Select(@event => @event.Id),
                .. located.Slice.ReadModels.Select(model => model.Id), .. located.Slice.Projections.Select(projection => projection.Id), .. located.Slice.Queries.Select(query => query.Id), .. SemanticStateViewArtifactRenderer.SelfReducerSources(located, context)]
        };
    }
}
