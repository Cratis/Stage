// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the own public events a slice declares into the contracts project, in the namespace they always had.
/// </summary>
internal static class SemanticEventContractsArtifactRenderer
{
    /// <summary>
    /// Renders the contracts file of one slice, which is none when the slice declares no own public event.
    /// </summary>
    /// <param name="located">The located semantic slice.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated contracts source, if any.</returns>
    internal static IEnumerable<RenderedFile> Render(LocatedSemanticSlice located, SemanticApplicationContext context)
    {
        if (!context.EventContracts.Any || context.EventContracts.Blockers(context).Any()) yield break;
        var events = located.Slice.Events.Where(@event => context.EventContracts.ContainsEvent(@event.Id)).ToArray();
        if (events.Length == 0) yield break;

        var builder = context.ContractsBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        var types = new SemanticTypeSystem(context);
        foreach (var @event in events)
        {
            SemanticStateChangeArtifactRenderer.RenderEvent(builder, @event, types, context);
        }

        var path = Path.Combine([context.ContractsFolder, .. SliceNaming.FolderPath(located.Path), $"{Identifiers.ToPascalCase(located.Slice.Name)}.Events.cs"]);
        yield return new(path, builder.ToString()) { Sources = [located.Slice.Id, .. events.Select(@event => @event.Id)] };
    }
}
