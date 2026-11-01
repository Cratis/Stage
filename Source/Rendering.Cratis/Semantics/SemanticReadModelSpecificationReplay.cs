// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Selects and renders the observed command facts that build a read model.
/// </summary>
internal static class SemanticReadModelSpecificationReplay
{
    /// <summary>
    /// Selects command facts for a read-model expectation.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="expected">The expected read-model state.</param>
    /// <param name="observation">The resolved observation path.</param>
    /// <returns>The produced and expected event pairs.</returns>
    internal static IReadOnlyList<(SemanticProducedEvent Produced, SemanticSpecificationEvent Expected)> For(
        SemanticApplication application,
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected,
        Observation observation) =>
        observation.Reducer is null
            ? SemanticProjectionSpecificationEvents.Replay(application, specification, observation.Projection!, observation.Command)
            : SemanticReducerSpecificationEvents.Replay(application, specification, expected, observation.Reducer, observation.Command);

    /// <summary>
    /// Selects command facts for absent or seeded read-model expectations.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="command">The observed command.</param>
    /// <returns>The produced and expected event pairs.</returns>
    internal static IReadOnlyList<(SemanticProducedEvent Produced, SemanticSpecificationEvent Expected)> ForAbsence(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        SemanticCommand command) =>
        specification.ThenNoEvents || specification.ThenDenied || !specification.ThenErrors.IsEmpty ? [] :
            [.. SemanticProductionSelection.Taken(context.Application, command, specification).Select((produced, index) => (produced,
                specification.ThenEventsInAnyOrder
                    ? specification.ThenEvents.Single(expected => expected.EventContract == produced.EventContract)
                    : specification.ThenEvents[index]))];

    /// <summary>
    /// Renders replay facts in their selected order.
    /// </summary>
    /// <param name="scope">The shared source-rendering state.</param>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="command">The observed command.</param>
    /// <param name="replay">The selected produced and expected event pairs.</param>
    /// <returns>Whether the rendered values require the Common namespace.</returns>
    internal static bool Render(
        SemanticReadModelSpecificationScope scope,
        SemanticSpecification specification,
        SemanticCommand? command,
        IReadOnlyList<(SemanticProducedEvent Produced, SemanticSpecificationEvent Expected)> replay)
    {
        var (builder, types, context, located) = scope;
        var needsCommon = false;
        foreach (var (produced, expectedEvent) in replay)
        {
            var @event = context.Events[produced.EventContract];
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path);
            if (!string.Equals(eventNamespace, SliceNaming.Namespace(context.RootNamespace, located.Path), StringComparison.Ordinal))
            {
                builder.Using(eventNamespace);
            }

            var source = SemanticDestinations.ForSpecification(specification, command!, produced);
            needsCommon |= SemanticTypeSystem.ValueNeedsCommon(source.Value, source.Type) ||
                @event.Properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(
                    expectedEvent.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            var arguments = @event.Properties.Select(property =>
                types.Value(expectedEvent.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            builder.Line($"await _scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type)}){SemanticSpecificationRouteRendering.Seed(SemanticSpecificationRouteRendering.ForCommand(specification, context, produced), context, types)}.Events(new {types.EventType(@event)}({string.Join(", ", arguments)}));");
        }

        return needsCommon;
    }

    /// <summary>
    /// Groups the resolved projection or reducer and the observed command.
    /// </summary>
    /// <param name="Projection">The projection, when present.</param>
    /// <param name="Reducer">The reducer, when present.</param>
    /// <param name="Command">The observed command.</param>
    internal sealed record Observation(SemanticProjection? Projection, SemanticReducer? Reducer, SemanticCommand Command);
}
