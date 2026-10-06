// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Refuses evolved events in the selected scope and its event dependencies until migrations can render.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    static void ValidateEventRevisions(
        SemanticApplicationContext context,
        IReadOnlyList<LocatedSemanticSlice> slices,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        var required = slices.SelectMany(located => EventDependencies(located.Slice, context))
            .Concat(SelectedConstraints(context, slices).SelectMany(selected =>
                selected.Constraint.Targets.Select(target => target.EventContract).Concat(selected.Constraint.ReleasedBy)))
            .ToHashSet();
        foreach (var @event in context.Events.Values.Where(@event => required.Contains(@event.Id) && @event.Revision != EventContractRevision.Initial))
        {
            diagnostics.Add(Error(
                "STAGE-ESM-026",
                $"Event '{@event.Name}' has evolved to revision {@event.Revision.Value}; Stage does not render event migrations yet.",
                @event.Id));
        }
    }

    static IEnumerable<SemanticId> EventDependencies(SemanticSlice slice, SemanticApplicationContext context) =>
        slice.Events.Select(@event => @event.Id)
            .Concat(slice.Commands.SelectMany(command => command.Produces).Select(produced => produced.EventContract))
            .Concat(slice.Projections.SelectMany(ProjectionReferencedEventNamesAreUnique.Contracts))
            .Concat(slice.Reducers.SelectMany(reducer => reducer.Transitions).Select(transition => transition.EventContract))
            .Concat(slice.Specifications.SelectMany(specification => specification.GivenEvents.Concat(specification.ThenEvents)).Select(@event => @event.EventContract))
            .Concat(slice.Specifications.Where(specification => specification.WhenAppended is not null).Select(specification => specification.WhenAppended!.EventContract))
            .Concat(slice.Specifications.Where(specification => specification.When is not null)
                .SelectMany(specification => context.Commands[specification.When!.Command].Produces).Select(produced => produced.EventContract));
}
