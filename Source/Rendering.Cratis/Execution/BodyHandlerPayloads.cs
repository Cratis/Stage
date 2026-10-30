// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>Writes a handler's returned event or event tuple as portable payloads in <c lang="csharp">produces</c> order.</summary>
internal static class BodyHandlerPayloads
{
    /// <summary>Converts the handler result into one composite per produced event.</summary>
    /// <param name="result">The single event, or a tuple of events.</param>
    /// <param name="unit">The unit's member and enum maps.</param>
    /// <returns>An array of event composites in declaration order.</returns>
    /// <exception cref="ArgumentException">The handler returned null or a null event.</exception>
    internal static SemanticValue Write(object? result, BodyUnitEntry unit)
    {
        if (result is null) throw new ArgumentException("A command handler must return its declared events.");

        // ITuple flattens a ValueTuple's Rest, so an eight-or-more tuple yields every element in order.
        var events = result is ITuple tuple ? Enumerable.Range(0, tuple.Length).Select(index => tuple[index]) : [result];
        return SemanticValue.Array([.. events.Select(@event => @event is null
            ? throw new ArgumentException("A command handler returned a null event.")
            : BodyStateWriter.Write(@event, unit))]);
    }
}
