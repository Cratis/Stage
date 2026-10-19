// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits a specification that appends an event the event log refuses because of a constraint.
/// </summary>
/// <remarks>
/// <c>when append &lt;event&gt;</c> followed by <c>then error "&lt;the constraint's message&gt;"</c> asserts that the log, not a command or a reaction, turns the append away.
/// The generated scenario seeds the given events and appends the event through the in-process event log, which enforces the constraints the application declares,
/// then asserts that the append failed with that constraint and that nothing else was appended.
/// </remarks>
internal static partial class SemanticSpecificationAdmission
{
    /// <summary>
    /// Finds the constraint a specification expects the appended event to be refused by.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>The constraint, or <see langword="null"/> when the specification does not assert that the append itself is refused by exactly one constraint.</returns>
    internal static SemanticConstraint? AppendRefusedConstraint(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.WhenAppended is not { } append || specification.WhenRedelivered is not null || specification.ThenErrors.Length != 1)
        {
            return null;
        }

        var error = specification.ThenErrors[0];
        var matches = context.Constraints.Select(_ => _.Constraint)
            .Where(constraint => constraint.Targets.Any(target => target.EventContract == append.EventContract) &&
                (error.Code == constraint.Name || (error.Code is null && error.Message == MessageFor(constraint))))
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }

    static string? AppendRefusalRejection(SemanticApplicationContext context, SemanticSpecification specification, SemanticConstraint constraint)
    {
        var append = specification.WhenAppended!;
        if (!specification.ThenEvents.IsEmpty || specification.ThenNoEvents || specification.ThenDenied || specification.ThenEventsInAnyOrder || specification.ThenReturns is not null ||
            HasReactionReadExpectations(specification) || specification.GivenCaller is not null || specification.GivenClock is not null)
        {
            return "An append refused by a constraint is asserted by its error alone: no events, response, read model, caller or clock expectation can accompany it.";
        }

        if (append.EventSource is null || append.Route is not null || specification.GivenEvents.Any(given => given.Route is not null || given.EventSource is null))
        {
            return "An append refused by a constraint needs an explicit, unrouted event source for the appended and the given events.";
        }

        if (!HasRenderableReactionFixtures(context, specification) || !HasRenderableReactionSource(context, append.EventSource) ||
            specification.GivenEvents.Any(given => !HasRenderableReactionSource(context, given.EventSource)))
        {
            return "The append or given events cannot be represented faithfully by a scenario.";
        }

        if (GivenEventsViolateConstraints(context, specification))
        {
            return "Given events violate a selected append-time constraint and cannot be seeded faithfully into Chronicle's event log.";
        }

        var constrained = constraint.Targets.Where(target => target.EventContract == append.EventContract).ToArray();
        if (constrained.Length != 1 || (!constrained[0].Properties.IsDefaultOrEmpty && constrained[0].Properties.Length > 1))
        {
            return $"Constraint '{constraint.Name}' constrains the appended event by more than one property; only the whole event or a single property can be proven here.";
        }

        return AppendViolates(constraint, specification) ? null : $"The given events do not make the append violate constraint '{constraint.Name}'.";
    }

    static bool AppendViolates(SemanticConstraint constraint, SemanticSpecification specification)
    {
        var append = specification.WhenAppended!;
        var target = constraint.Targets.Single(candidate => candidate.EventContract == append.EventContract);
        if (append.EventSource?.Value is not SemanticTextValue appendSource)
        {
            return false;
        }

        var sameContract = specification.GivenEvents.Where(given => given.EventContract == append.EventContract).ToArray();
        if (constraint.Kind == SemanticConstraintKind.UniqueEventOccurrence)
        {
            return sameContract.Any(given => given.EventSource?.Value is SemanticTextValue source && source.Value == appendSource.Value);
        }

        if (target.Properties.IsDefaultOrEmpty)
        {
            return false;
        }

        var property = target.Properties[0];
        if (append.Values.FirstOrDefault(value => value.TargetProperty == property)?.Value is not SemanticTextValue appended)
        {
            return false;
        }

        string Normalize(string value) => constraint.IgnoreCasing ? value.ToLowerInvariant() : value;

        return sameContract.Any(given => given.EventSource?.Value is SemanticTextValue source && source.Value != appendSource.Value &&
            given.Values.FirstOrDefault(value => value.TargetProperty == property)?.Value is SemanticTextValue held && Normalize(held.Value) == Normalize(appended.Value));
    }
}
