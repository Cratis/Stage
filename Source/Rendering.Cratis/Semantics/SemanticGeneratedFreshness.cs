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
        !context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15) ? [] : command.Produces
            .Select(produced => SemanticDestinations.Of(command, produced)).OfType<SemanticResolvedExpression>()
            .Select(destination => command.Properties.Single(property => property.Id == destination.Target))
            .Where(property => property.IsGenerated).DistinctBy(property => property.Id);

    internal static string Events(SemanticCommand command, SemanticApplicationContext context, Func<SemanticProducedEvent, string> wrappedEvent)
    {
        var types = new SemanticTypeSystem(context);
        var scopes = Destinations(command, context).Select(property =>
        {
            var source = types.EventSourceExpression(SemanticCommandResultRendering.PropertyValue(property), property.Type);

            // Replace these explicit scopes when Arc's generated-identifier freshness contract ships (Arc#3129).
            return $"new({source}, new {Sequences}.Concurrency.ConcurrencyScope(global::Cratis.Chronicle.Events.EventSequenceNumber.BeforeFirst, EventSourceId: {source}))";
        });

        return $"new {ResultType}([{string.Join(", ", command.Produces.Select(wrappedEvent))}], [{string.Join(", ", scopes)}])";
    }
}
