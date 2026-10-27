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

        ValidateCode(gap.Code);
        return "// TODO " + JavaLiterals.EscapeLineComment(gap.Code + ": " + gap.Construct + ": " + gap.Intent) + "\n" + placeholder;
    }

    /// <summary>Validates caller-owned degradation codes without accepting #807's reserved renderer codes.</summary>
    /// <param name="code">The existing degradation code.</param>
    /// <exception cref="LanguagePackRefusal">The code is empty, outside the namespace, or reserved by #807.</exception>
    public static void ValidateCode(string code)
    {
        const string prefix = "STAGE-DEGRADE-";
        if (!code.StartsWith(prefix, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(code[prefix.Length..]) ||
            int.TryParse(code[prefix.Length..], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number) && number is >= 1 and <= 5)
            throw new LanguagePackRefusal(JavaRefusalCodes.InvalidGapCode, "A Java gap requires a non-empty STAGE-DEGRADE code outside #807's reserved 001–005 range.");
    }
}
