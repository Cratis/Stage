// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static void ValidateEventTypeIdentities(
        SemanticApplicationContext context,
        IReadOnlyList<LocatedSemanticSlice> slices,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        var selected = slices.SelectMany(located => located.Slice.Events).Select(@event => @event.Id).ToHashSet();
        var collisions = context.Events.Values.GroupBy(@event =>
            (Id: @event.StoredName ?? Identifiers.ToPascalCase(@event.Name), Generation: @event.Revision.Value))
            .Where(group => group.Count() > 1 && group.Any(@event => selected.Contains(@event.Id)));
        foreach (var collision in collisions)
        {
            var names = string.Join(", ", collision.Select(@event => $"'{@event.Name}'").Order(StringComparer.Ordinal));
            diagnostics.Add(Error(
                "STAGE-ESM-012",
                $"Events {names} collide on generated Chronicle event type id '{collision.Key.Id}' at generation {collision.Key.Generation}; stored names and unpinned generated C# names must be unique within a generation.",
                collision.First(@event => selected.Contains(@event.Id)).Id));
        }
    }
}
