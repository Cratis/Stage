// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Execution;

internal static partial class BodyUnitSources
{
    /// <summary>Renders the single closed unit the renderer's admission also compiles for a handler.</summary>
    /// <param name="context">The loaded application.</param>
    /// <param name="command">The command that owns the handler.</param>
    /// <returns>The source and its entry point identity.</returns>
    internal static HandlerSources RenderHandler(SemanticApplicationContext context, SemanticCommand command)
    {
        var suffix = SemanticCommandHandlerRendering.Suffix(command);
        var unit = $"HandlerUnit_{suffix}";
        var ns = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(command.Id).Path);
        return new(SemanticCommandBodyAdmission.HandlerSource(command, context, unit), $"worker/handlers/{unit}.cs", $"{ns}.{unit}", SemanticCommandHandlerRendering.Method(command, context), $"{ns}.{unit}+{SemanticCommandHandlerRendering.SnapshotType(command, context)}");
    }

    internal sealed record HandlerSources(string Source, string Path, string EntryType, string EntryMethod, string SnapshotType);
}
