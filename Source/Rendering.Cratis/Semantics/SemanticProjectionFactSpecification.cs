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
/// Without seeds the append is the last history fact. With seeds it stays separate so the generated scenario
/// replaces the seeded keys' history before folding the append over their fixture state. Neither case needs
/// the reaction scenarios, which do not materialize read models.
/// </remarks>
internal static class SemanticProjectionFactSpecification
{
    /// <summary>
    /// Lowers a projection-only specification while retaining a seeded append's position after its fixtures.
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
            (specification.ThenReadModels.IsEmpty && specification.ThenAbsentReadModels.IsEmpty) || !specification.ThenEvents.IsEmpty || specification.ThenNoEvents ||
            specification.ThenEventsInAnyOrder || !specification.ThenQueries.IsEmpty || !specification.ThenErrors.IsEmpty ||
            specification.ThenDenied || specification.ThenReturns is not null ||
            (!specification.GivenReadModels.IsEmpty && specification.ThenAbsentReadModels.IsEmpty))
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

        // An unsourced removal has no observable source when every observing projection keys it
        // solely by its event property. Use that property as the concrete scenario's required source.
        if (appended.EventSource is null)
        {
            appended = appended with { EventSource = PropertyRemovalSource(context, appended.EventContract, appended.Values) };
        }
        lowered = specification with { WhenAppended = appended };
        if (specification.GivenReadModels.IsEmpty) lowered = WithAppendedFact(lowered);
        return true;
    }

    internal static SemanticSpecification WithAppendedFact(SemanticSpecification specification) =>
        specification.WhenAppended is not { } appended ? specification : specification with
        {
            WhenAppended = null,
            GivenEvents = specification.GivenEvents.Add(new(appended.EventContract, appended.Values) { EventSource = appended.EventSource, Route = appended.Route })
        };

    static SemanticEventSourceIdentity? PropertyRemovalSource(
        SemanticApplicationContext context,
        SemanticId eventContract,
        System.Collections.Immutable.ImmutableArray<SemanticPropertyValue> values)
    {
        // All blocks observe the fact even when they do not name its contract, so an invented source is visible to them.
        if (context.Projections.Values.Any(projection => ObservesAllEvents(projection.Scope))) return null;
        var projections = context.Projections.Values.Where(projection =>
            Projections.ProjectionReferencedEventNamesAreUnique.Contracts(projection).Contains(eventContract)).ToArray();
        if (projections.Length == 0 || context.Reducers.Any(reducer => reducer.Transitions.Any(transition => transition.EventContract == eventContract)) ||
            projections.Any(projection => projection.Scope is not { Every: null } scope ||
                !scope.Removals.Any(removal => removal.EventContract == eventContract && removal.ParentKey is null &&
                    removal.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path.Length: 1 } })))
        {
            return null;
        }
        var removal = projections[0].Scope!.Removals.Single(removal => removal.EventContract == eventContract);
        var propertyId = ((SemanticProjectionEventProperty)((SemanticProjectionValueKey)removal.Key).Value).Path[0];
        var property = context.Events[eventContract].Properties.SingleOrDefault(property => property.Id == propertyId);
        if (property is null) return null;
        var value = values.SingleOrDefault(value => value.TargetProperty == property.Id)?.Value;
        return value is null || property.Type.IsOptional || !IsTextKey(context, property.Type) ? null : new(property.Type, value);
    }

    static bool ObservesAllEvents(SemanticProjectionScope? scope) =>
        scope is not null && (scope.Every is { SubscribesToAllEvents: true } ||
            scope.Children.Any(child => ObservesAllEvents(child.Scope)) || scope.Nested.Any(nested => ObservesAllEvents(nested.Scope)));

    static bool IsTextKey(SemanticApplicationContext context, SemanticTypeReference type) =>
        !type.IsCollection && (type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[type.Target].Primitive : type.Primitive)
            is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid;
}
