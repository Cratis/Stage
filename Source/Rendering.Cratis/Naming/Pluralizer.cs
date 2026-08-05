// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Pluralizes English words for use in generated query method names (e.g. <c>AllAuthors</c>).
/// </summary>
public static class Pluralizer
{
    /// <summary>
    /// Pluralizes a word.
    /// </summary>
    /// <param name="word">The singular word.</param>
    /// <returns>The pluralized word.</returns>
    public static string Pluralize(string word)
    {
        if (word.Length == 0)
        {
            return word;
        }

        if (word.EndsWith('y') && word.Length > 1 && !IsVowel(word[^2]))
        {
            return $"{word[..^1]}ies";
        }

        if (word.EndsWith('s') || word.EndsWith('x') || word.EndsWith('z') ||
            word.EndsWith("ch", StringComparison.Ordinal) || word.EndsWith("sh", StringComparison.Ordinal))
        {
            return word + "es";
        }

        return word + "s";
    }

    static bool IsVowel(char c) => "aeiouAEIOU".Contains(c);
}
