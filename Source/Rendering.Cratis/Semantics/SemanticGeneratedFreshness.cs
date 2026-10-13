// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders atomic freshness scopes for generated event destinations at ESM v15.
/// </summary>
internal static class SemanticGeneratedFreshness
{
    internal const string ResultType = $"{Sequences}.EventsWithConcurrencyScopes";
    const string Sequences = "global::Cratis.Chronicle.EventSequences";

    internal static IEnumerable<SemanticProperty> Destinations(SemanticCommand command, SemanticApplicationContext context) =>
        !context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15) || command.Properties.All(property => !property.IsGenerated) ? [] : command.Produces
            .Select(produced => SemanticDestinations.Of(command, produced)).OfType<SemanticResolvedExpression>()
            .Select(destination => command.Properties.Single(property => property.Id == destination.Target))
            .Where(property => Primitive(property, context) is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text).DistinctBy(property => property.Id);

    internal static string Events(SemanticGeneratedFreshnessContext selection)
    {
        var (command, context, wrappedEvent, selectedEvents, inclusion) = selection;
        var types = new SemanticTypeSystem(context);
        const string sourceType = "global::Cratis.Chronicle.Events.EventSourceId";
        var sources = Destinations(command, context).Select((property, index) =>
        {
            var source = types.EventSourceExpression(SemanticCommandResultRendering.PropertyValue(property), property.Type);

            if (inclusion is null) return property.IsGenerated ? source : $".. ({MatchesGenerated(property, index, command, context)} ? new {sourceType}[] {{ {source} }} : [])";
            var guard = inclusion(property);
            var predicate = property.IsGenerated ? guard : $"({guard}) && ({MatchesGenerated(property, index, command, context)})";
            return $".. ({predicate} ? new {sourceType}[] {{ {source} }} : [])";
        });

        // Replace these explicit scopes when Arc's generated-identifier freshness contract ships (Arc#3129).
        var distinct = $"global::System.Linq.Enumerable.Distinct<{sourceType}>([{string.Join(", ", sources)}])";
        var scopes = $"global::System.Linq.Enumerable.Select({distinct}, source => new global::System.Collections.Generic.KeyValuePair<{sourceType}, {Sequences}.Concurrency.ConcurrencyScope>(source, new(global::Cratis.Chronicle.Events.EventSequenceNumber.BeforeFirst, EventSourceId: source)))";

        return $"new {ResultType}({selectedEvents ?? $"[{string.Join(", ", command.Produces.Select(wrappedEvent))}]"}, {scopes})";
    }

    static string MatchesGenerated(SemanticProperty destination, int index, SemanticCommand command, SemanticApplicationContext context) =>
        string.Join(" || ", command.Properties.Where(property => property.IsGenerated).Select((property, generatedIndex) =>
        {
            var value = Value(destination);
            var generated = Value(property);
            if (Primitive(property, context) != SemanticPrimitiveType.Uuid)
            {
                return $"global::System.String.Equals({value}.ToString(), {generated}, global::System.StringComparison.Ordinal)";
            }
            if (Primitive(destination, context) == SemanticPrimitiveType.Uuid) return $"{value} == {generated}";
            var parsed = $"generatedSource{index}_{generatedIndex}";

            return $"(global::System.Guid.TryParse({value}, out var {parsed}) && {parsed} == {generated})";
        }));

    static string Value(SemanticProperty property) => SemanticCommandResultRendering.PropertyValue(property) +
        (property.Type.Kind == SemanticTypeReferenceKind.Concept ? ".Value" : string.Empty);

    static SemanticPrimitiveType Primitive(SemanticProperty property, SemanticApplicationContext context) =>
        property.Type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[property.Type.Target].Primitive : property.Type.Primitive;
}
