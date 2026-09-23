// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The exception that is thrown when the Cratis ESM renderer encounters an unhandled semantic variant.
/// </summary>
public sealed class UnsupportedSemanticRendering : Exception
{
    UnsupportedSemanticRendering(string message) : base(message)
    {
    }

    /// <summary>
    /// Gets the diagnostic code for an unhandled semantic variant.
    /// </summary>
    public string Code => "STAGE-ESM-012";

    /// <summary>
    /// Creates a failure naming the semantic variant that cannot be rendered.
    /// </summary>
    /// <param name="category">The semantic category.</param>
    /// <param name="value">The unhandled value.</param>
    /// <returns>The typed failure.</returns>
    public static UnsupportedSemanticRendering For(string category, object value) =>
        new($"The Cratis ESM renderer cannot handle {category} '{value}'.");
}
