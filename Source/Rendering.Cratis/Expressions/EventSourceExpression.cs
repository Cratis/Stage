// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Expressions;

/// <summary>
/// Converts a scalar destination with Arc's event-source value semantics.
/// </summary>
internal static class EventSourceExpression
{
    /// <summary>
    /// Preserves implicit identity conversions, otherwise converts the scalar to an event-source string.
    /// </summary>
    /// <param name="expression">The rendered destination expression.</param>
    /// <param name="hasImplicitConversion">Whether the value converts directly to an event source.</param>
    /// <returns>The event-source expression.</returns>
    public static string Render(string expression, bool hasImplicitConversion) => hasImplicitConversion
        ? expression
        : $"new global::Cratis.Chronicle.Events.EventSourceId(({expression}).ToString())";
}
