// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders canonical Date and DateTime literals as exact values, without parsing at runtime.
/// </summary>
internal static class SemanticDateLiteral
{
    /// <summary>
    /// Checks whether text is a literal the renderer can reproduce exactly for the primitive.
    /// </summary>
    /// <param name="text">The literal text.</param>
    /// <param name="primitive">The Date or DateTime primitive.</param>
    /// <returns>Whether the text is a calendar date or an explicitly offset instant.</returns>
    internal static bool IsCanonical(string text, SemanticPrimitiveType primitive) => primitive == SemanticPrimitiveType.Date
        ? DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
        : SemanticDateComparisons.TryParseInstant(text, out _);

    /// <summary>
    /// Renders a calendar date as a DateOnly, or an instant as a UTC-offset DateTimeOffset holding every tick.
    /// </summary>
    /// <param name="text">The canonical literal text.</param>
    /// <param name="primitive">The Date or DateTime primitive.</param>
    /// <param name="name">Names a type from the System namespace in the destination file.</param>
    /// <returns>The C# expression.</returns>
    /// <exception cref="UnsupportedSemanticRendering">The text is not a canonical literal of the primitive.</exception>
    internal static string Render(string text, SemanticPrimitiveType primitive, Func<string, string> name)
    {
        if (primitive == SemanticPrimitiveType.Date)
        {
            if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) throw UnsupportedSemanticRendering.For(nameof(SemanticDateLiteral), text);

            return $"new {name("DateOnly")}({date.Year}, {date.Month}, {date.Day})";
        }

        if (!SemanticDateComparisons.TryParseInstant(text, out var instant)) throw UnsupportedSemanticRendering.For(nameof(SemanticDateLiteral), text);

        return $"new {name("DateTimeOffset")}({instant.UtcTicks.ToString(CultureInfo.InvariantCulture)}L, {name("TimeSpan")}.Zero)";
    }
}
