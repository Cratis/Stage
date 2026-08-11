// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the Screenplay <c>produces</c> declarations of a command into the Stage <see cref="ProducedEvent"/> records
/// the engine evaluates at runtime to build and append event payloads.
/// </summary>
public static class ProducesConverter
{
    /// <summary>
    /// Converts a command's produces declarations into their Stage records.
    /// </summary>
    /// <param name="produces">The produces declarations.</param>
    /// <returns>The Stage produced-event definitions, in declaration order.</returns>
    public static IReadOnlyList<ProducedEvent> Convert(IEnumerable<ProducesSyntax> produces) =>
    [
        .. produces.Select(declaration => new ProducedEvent(
            declaration.Event,
            ConditionConverter.Convert(declaration.When),
            [.. declaration.Mappings.Select(Property)],
            ProducedValueConverter.Tags(declaration.Tags))
        {
            For = EventSource(declaration.For)
        })
    ];

    // A 'for' expression the engine has no way to evaluate would silently redirect the append to the command's own
    // event source, which is a different stream than the document asked for — so it is left off instead.
    static ProducedEventSource? EventSource(ExpressionSyntax? expression)
    {
        if (expression is null)
        {
            return null;
        }

        var (kind, text) = ProducedValueConverter.Convert(expression);

        return kind is ProducedValueKind.Unsupported ? null : new ProducedEventSource(kind, text);
    }

    static ProducedEventProperty Property(PropertyMappingSyntax mapping)
    {
        var (kind, expression) = ProducedValueConverter.Convert(mapping.Source);

        return new(mapping.Property, kind, expression);
    }
}
