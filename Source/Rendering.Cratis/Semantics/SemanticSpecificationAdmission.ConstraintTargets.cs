// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    /// <summary>
    /// Gets whether a specification with invocation expectations appends a constraint-target event and records the invocations after the event log has enforced that append.
    /// </summary>
    /// <param name="context">The indexed application.</param>
    /// <param name="specification">The specification.</param>
    /// <param name="selected">The selected reaction trigger.</param>
    /// <returns>Whether the recording scenario appends through the event log instead of a reactor scenario.</returns>
    internal static bool UsesRecordingAppendScenario(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected) =>
        !specification.ThenInvocations.IsDefaultOrEmpty && !HasRefusalOutcome(specification) && !UsesRefusalScenario(specification, selected) &&
        IsReactionConstraintTarget(context, SemanticReactionSpecification.Input(specification).EventContract);

    static string? ConstraintTargetAppendRejection(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected)
    {
        var append = SemanticReactionSpecification.Input(specification);
        if (!IsReactionConstraintTarget(context, append.EventContract)) return null;
        if (!UsesRefusalScenario(specification, selected) && !UsesRecordingAppendScenario(context, specification, selected))
            return "The appended event is an append-time constraint target; a reactor scenario does not enforce that append.";
        if (append.EventSource is null || append.Route is not null || specification.GivenEvents.Any(given => given.Route is not null))
            return "A constraint-target append is enforced by the event log and requires an explicit append source and unrouted fixtures.";

        // The log refuses the trigger when it conflicts with a given occurrence; the scenario states it as accepted, so a conflicting pair cannot be rendered.
        var occurrences = specification.GivenEvents.Add(new(append.EventContract, append.Values) { EventSource = append.EventSource });
        return GivenEventsViolateConstraints(context, specification with { GivenEvents = occurrences })
            ? "The given events and the appended event violate an append-time constraint, which the event log would refuse." : null;
    }
}
