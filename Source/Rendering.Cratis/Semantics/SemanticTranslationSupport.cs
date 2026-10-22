// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Describes the portable translation shapes shared by execution and rendering.
/// Condition rendering is deliberately separate from structural admission.
/// </summary>
internal static class SemanticTranslationSupport
{
    /// <summary>Determines whether a capture consumes foreign events into local facts.</summary>
    /// <param name="capture">The capture.</param>
    /// <param name="events">The application's event contracts.</param>
    /// <returns>Whether its record-level structure is supported.</returns>
    internal static bool Capture(SemanticCapture capture, IReadOnlyDictionary<SemanticId, SemanticEventContract> events) =>
        capture.EventsSource is { Events.IsDefaultOrEmpty: false } source &&
        !string.IsNullOrWhiteSpace(capture.Key) &&
        capture.Map.IsDefaultOrEmpty && capture.Children.IsDefaultOrEmpty && capture.Nested.IsDefaultOrEmpty &&
        source.Events.All(id => events.TryGetValue(id, out var contract) && contract.Origin is not null && contract.Properties.Any(property => property.Name == capture.Key)) &&
        capture.Appends.All(append => events.TryGetValue(append.EventContract, out var contract) &&
            contract.Origin is null && contract.Visibility == SemanticEventVisibility.Private &&
            append.Mappings.All(mapping => mapping.Field is not null || mapping.Value is SemanticValueExpression));

    /// <summary>Determines whether a projection publishes a complete public event per source fact.</summary>
    /// <param name="projection">The projection.</param>
    /// <param name="events">The application's event contracts.</param>
    /// <returns>Whether its event-target structure is supported.</returns>
    internal static bool Projection(SemanticProjection projection, IReadOnlyDictionary<SemanticId, SemanticEventContract> events) =>
        projection.Target == SemanticProjectionTargetKind.Event &&
        events.TryGetValue(projection.ReadModel, out var target) && target.Visibility == SemanticEventVisibility.Public && target.Origin is null &&
        projection.Scope is { } scope && !scope.From.IsDefaultOrEmpty &&
        scope.Every is null && scope.Joins.IsDefaultOrEmpty && scope.Children.IsDefaultOrEmpty && scope.Nested.IsDefaultOrEmpty &&
        scope.Removals.IsDefaultOrEmpty && scope.JoinRemovals.IsDefaultOrEmpty &&
        scope.From.All(from => events.TryGetValue(from.EventContract, out var source) && source.Origin is null && source.Visibility == SemanticEventVisibility.Private &&
            from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity } && from.ParentKey is null &&
            from.Mappings.All(mapping => mapping.Target.Length == 1 && mapping.Operation == SemanticProjectionOperation.Set && mapping.Factor is null &&
                (mapping.Source is SemanticProjectionLiteral or SemanticProjectionEventProperty or SemanticProjectionEventSourceIdentity ||
                    (mapping.Source is SemanticProjectionEventContextValue context && context.Path == "eventSourceId")) &&
                (mapping.Source is not SemanticProjectionEventProperty property || (!property.Path.IsDefaultOrEmpty && source.Properties.Any(candidate => candidate.Id == property.Path[0])))) &&
            target.Properties.Where(property => !property.Type.IsOptional).All(property => from.Mappings.Any(mapping => mapping.Target[0] == property.Id)));

    /// <summary>Determines whether a translation slice contains only supported translation structures.</summary>
    /// <param name="slice">The slice.</param>
    /// <param name="events">The application's event contracts.</param>
    /// <returns>Whether translation admission may omit the generic slice refusal.</returns>
    internal static bool Slice(SemanticSlice slice, IReadOnlyDictionary<SemanticId, SemanticEventContract> events) =>
        slice.Kind == SemanticSliceKind.Translate &&
        slice.Reactions.IsDefaultOrEmpty &&
        (slice.Captures.IsDefaultOrEmpty || slice.Captures.All(capture => Capture(capture, events))) &&
        slice.Projections.All(projection => projection.Target != SemanticProjectionTargetKind.Event || Projection(projection, events)) &&
        slice.Reducers.All(reducer => reducer.Target != SemanticProjectionTargetKind.Event);

    /// <summary>Determines whether a foreign or public contract participates in a supported translation.</summary>
    /// <param name="contract">The event contract.</param>
    /// <param name="slices">The application's slices.</param>
    /// <param name="events">The application's event contracts.</param>
    /// <returns>Whether the special event contract is supported.</returns>
    internal static bool Event(SemanticEventContract contract, IEnumerable<SemanticSlice> slices, IReadOnlyDictionary<SemanticId, SemanticEventContract> events) =>
        slices.Any(slice => Slice(slice, events) && (contract.Origin is not null
            ? slice.Captures.Any(capture => capture.EventsSource!.Events.Contains(contract.Id))
            : contract.Visibility == SemanticEventVisibility.Public && slice.Projections.Any(projection => projection.ReadModel == contract.Id && Projection(projection, events))));
}
