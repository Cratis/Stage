// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Defines the reserved and contextual names protected by java@1, pinned to JDK 21.
/// </summary>
public static class JavaIdentifiers
{
    static readonly FrozenSet<string> _reserved =
        ("abstract assert boolean break byte case catch char class const continue default do double else enum extends " +
         "final finally float for goto if implements import instanceof int interface long native new package private " +
         "protected public return short static strictfp super switch synchronized this throw throws transient try void " +
         "volatile while true false null _ exports module open opens provides requires to transitive uses with record " +
         "sealed permits var yield when non-sealed").Split(' ').ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Determines whether a name is reserved or contextual in the java@1 naming policy.
    /// </summary>
    /// <param name="name">The case-sensitive name.</param>
    /// <returns>Whether generated identifiers must avoid this name.</returns>
    public static bool IsReserved(string name) => _reserved.Contains(name);

    /// <summary>
    /// Determines whether a name satisfies java@1's ASCII identifier policy without '$'.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>Whether the name is non-reserved, at most 255 characters, and syntactically valid.</returns>
    public static bool IsIdentifier(string name) =>
        name.Length is > 0 and <= 255 && !IsReserved(name) &&
        (char.IsAsciiLetter(name[0]) || name[0] == '_') &&
        name.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');

    /// <summary>
    /// Determines whether a name can be one segment of a structured symbol or package.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>Whether the name matches <c language="java">[A-Za-z_][A-Za-z0-9_]*</c>, is not reserved and is not <c language="java">_</c>.</returns>
    public static bool IsSymbolSegment(string name) =>
        name.Length > 0 && name != "_" && !IsReserved(name) &&
        (char.IsAsciiLetter(name[0]) || name[0] == '_') &&
        name.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');

    /// <summary>
    /// Sanitizes a model name to the java@1 ASCII identifier policy.
    /// </summary>
    /// <param name="name">The model name.</param>
    /// <returns>A non-reserved Java identifier.</returns>
    /// <exception cref="LanguagePackRefusal">The input contains '$' or the result exceeds 255 characters.</exception>
    public static string Sanitize(string name)
    {
        if (name.Contains('$', StringComparison.Ordinal))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.InvalidIdentifier, "Java identifiers must not contain '$'.");
        }

        var sanitized = new string([.. name.Select(character => char.IsAsciiLetterOrDigit(character) || character == '_' ? character : '_')]);
        if (sanitized.Length == 0) sanitized = "Generated";
        if (char.IsAsciiDigit(sanitized[0])) sanitized = "_" + sanitized;
        if (IsReserved(sanitized)) sanitized += "_";
        if (!IsIdentifier(sanitized))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.InvalidIdentifier, "Java identifiers must not exceed 255 characters.");
        }

        return sanitized;
    }
}
