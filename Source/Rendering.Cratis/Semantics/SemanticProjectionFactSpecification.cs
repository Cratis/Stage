// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Recognises a specification that only projects facts: events given as history, optionally one more event appended
/// directly, and the read model state that follows. No command runs and no reaction is asked to settle.
/// </summary>
/// <remarks>
/// The appended event is just the last fact of the history. The reference runner projects the history and the append in
/// order and compares the read models, which is exactly what a generated read-model scenario does when it is given the
/// same events in the same order, so the append is lowered to a given event here instead of being routed through the
/// reaction scenarios, which do not materialize read models.
/// </remarks>
internal static class SemanticProjectionFactSpecification
{
    /// <summary>
    /// Lowers a projection-only specification to one whose history already contains the appended event.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="specification">The authored specification.</param>
    /// <param name="lowered">The specification with the appended event as its last given event and no action.</param>
    /// <returns>True when the specification asserts only projected read-model state over facts.</returns>
    public static bool TryLower(SemanticApplicationContext context, SemanticSpecification specification, [NotNullWhen(true)] out SemanticSpecification? lowered)
    {
        lowered = null;
        if (specification.When is not null || specification.WhenRedelivered is not null || specification.WhenCapture is not null ||
            specification.WhenTrigger is not null || specification.WhenClock is not null ||
            specification.ThenReadModels.IsEmpty || !specification.ThenEvents.IsEmpty || specification.ThenNoEvents ||
            specification.ThenEventsInAnyOrder || !specification.ThenQueries.IsEmpty || !specification.ThenErrors.IsEmpty ||
            specification.ThenDenied || specification.ThenReturns is not null || !specification.ThenAbsentReadModels.IsEmpty ||
            !specification.GivenReadModels.IsEmpty)
        {
            return false;
        }

        if (specification.WhenAppended is not { } appended)
        {
            lowered = specification;
            return true;
        }

        // An event something reacts to would have to be settled; an event an append-time constraint governs would have to be refused.
        if (SemanticSpecificationAdmission.EventReactionTriggers(context).Any(trigger => trigger.Source == appended.EventContract) ||
            context.Constraints.Any(selected => selected.Constraint.Targets.Any(target => target.EventContract == appended.EventContract)))
        {
            return false;
        }

        lowered = specification with
        {
            WhenAppended = null,
            GivenEvents = specification.GivenEvents.Add(new(appended.EventContract, appended.Values) { EventSource = appended.EventSource, Route = appended.Route })
        };
        return true;
    }
}
