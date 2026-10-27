// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Emits deterministic placeholders only in java@1's supported grammar contexts.
/// </summary>
public static class JavaPlaceholders
{
    /// <summary>
    /// Renders a semantic gap, retaining the caller's existing degradation code in a safe line comment.
    /// </summary>
    /// <param name="gap">The unsupported construct and its grammar context.</param>
    /// <returns>A placeholder with generated LF line endings.</returns>
    /// <exception cref="LanguagePackRefusal">The context is outside the kit contract.</exception>
    public static string Render(GapSegment gap)
    {
        var placeholder = gap.Context switch
        {
            "type" => "java.lang.Object",
            "member" => string.Empty,
            "statement" => "throw new java.lang.UnsupportedOperationException(" + JavaLiterals.EscapeString(gap.Code + ": " + gap.Construct + ": " + gap.Intent) + ");",
            _ => throw new LanguagePackRefusal(JavaRefusalCodes.UnsupportedContext, $"Unknown Java grammar context '{gap.Context}'."),
        };

        return "// TODO " + JavaLiterals.EscapeLineComment(gap.Code + ": " + gap.Construct + ": " + gap.Intent) + "\n" + placeholder;
    }
}
