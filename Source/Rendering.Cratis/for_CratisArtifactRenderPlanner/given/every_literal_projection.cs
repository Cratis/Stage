// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class every_literal_projection
{
    // Label is required and supplied only by every on the renamed event.
    // Registration deliberately collides with the literal to exercise last-write semantics.
    internal static readonly string Source = when_rendering_scoped_projections.ScopedSource
        .Replace("notes ProjectNote[]", "label String\n        notes ProjectNote[]", StringComparison.Ordinal)
        .Replace("projection ProjectSummaryProjection => ProjectSummary\n", "projection ProjectSummaryProjection => ProjectSummary\n        no automap\n", StringComparison.Ordinal)
        .Replace("name = name\n          increment visits", "name = name\n          label = \"local\"\n          increment visits", StringComparison.Ordinal)
        .Replace("lastSeen = $eventSourceId", "lastSeen = $eventSourceId\n          label = \"fixed\"", StringComparison.Ordinal)
        .Replace("from ProjectRenamed key projectId\n          name = name\n          label = \"local\"", "from ProjectRenamed\n          name = name", StringComparison.Ordinal)
        .Replace("        join project on projectId\n          with ProjectNamed\n            no automap\n            name = name\n", string.Empty, StringComparison.Ordinal)
        .Replace("        children notes identified by noteId", "          clear with ProjectRenamed\n          every\n            name = \"nested\"\n        children notes identified by noteId", StringComparison.Ordinal)
        .Replace("remove with ProjectNoteRemoved key noteId\n            parent projectId", "every\n            name = \"child\"\n          remove with ProjectNoteRemoved key noteId\n            parent projectId\n          remove via join on ProjectNoteRemovedViaJoin key noteId", StringComparison.Ordinal);
}
#endif
