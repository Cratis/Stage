// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Resolves where a command's facts land and which stream a specification names, the way the Screenplay
/// evaluator resolves them.
/// </summary>
internal static class SemanticDestinations
{
    /// <summary>
    /// Gets the effective destination expression of one produced event.
    /// </summary>
    /// <param name="command">The producing command.</param>
    /// <param name="produced">The produced event.</param>
    /// <returns>The per-occurrence destination, or the command's typed ESM v2 destination when there is none.</returns>
    public static SemanticExpression? Of(SemanticCommand command, SemanticProducedEvent produced) =>
        produced.Destination ?? command.Destination?.Value;

    /// <summary>
    /// Gets the explicit event sources an accepted specification states.
    /// </summary>
    /// <param name="specification">The specification.</param>
    /// <returns>The command and expected-event sources that are present.</returns>
    public static IEnumerable<SemanticEventSourceIdentity> Explicit(SemanticSpecification specification) =>
        new[] { specification.When?.EventSource }
            .Concat(specification.ThenEvents.Select(_ => _.EventSource))
            .OfType<SemanticEventSourceIdentity>();

    /// <summary>
    /// Gets the stream a specification expects an event on.
    /// </summary>
    /// <param name="specification">The admitted specification.</param>
    /// <param name="command">The command the specification executes.</param>
    /// <param name="produced">The produced event that is expected.</param>
    /// <returns>The explicit ESM v2 source when the specification states one; otherwise the command's destination value.</returns>
    public static SemanticEventSourceIdentity ForSpecification(
        SemanticSpecification specification,
        SemanticCommand command,
        SemanticProducedEvent produced)
    {
        // Admission guarantees every explicit source of one specification agrees, so the first one is the stream.
        if (Explicit(specification).FirstOrDefault() is { } source)
        {
            return source;
        }

        var destination = (SemanticResolvedExpression)Of(command, produced)!;
        var property = command.Properties.Single(_ => _.Id == destination.Target);
        return new(property.Type, specification.When!.Values.Single(_ => _.TargetProperty == destination.Target).Value);
    }
}
