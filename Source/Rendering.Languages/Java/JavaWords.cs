// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Extracts conservative ASCII words after JLS 3.3 decoding, including words in comments and literals.
/// </summary>
public static class JavaWords
{
    /// <summary>
    /// Unions two passes over the decoded text: identifier-ignorable characters separate words in one pass and are deleted in the other.
    /// </summary>
    /// <param name="assembledRaw">The complete raw text to scan.</param>
    /// <returns>All maximal ASCII letter, digit, underscore or dollar runs not starting with a digit, and those not followed by a dot after Java whitespace.</returns>
    /// <exception cref="LanguagePackRefusal">An eligible Unicode escape is malformed.</exception>
    public static JavaWordSet Scan(string assembledRaw)
    {
        var decoded = JavaUnicodeEscapes.Decode(assembledRaw);
        var all = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        var declaring = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        var qualifiers = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        ScanPass(decoded, all, declaring, qualifiers);
        ScanPass(JavaIdentifierIgnorables.Remove(decoded), all, declaring, qualifiers);
        return new(all.ToImmutable(), declaring.ToImmutable(), qualifiers.ToImmutable());
    }

    static void ScanPass(string text, ImmutableHashSet<string>.Builder all, ImmutableHashSet<string>.Builder declaring, ImmutableHashSet<string>.Builder qualifiers)
    {
        var index = 0;
        while (index < text.Length)
        {
            if (!IsWordCharacter(text[index]))
            {
                index++;
                continue;
            }

            var start = index++;
            while (index < text.Length && IsWordCharacter(text[index]))
            {
                index++;
            }

            if (!char.IsAsciiDigit(text[start]))
            {
                AddWord(text, start, index, all, declaring, qualifiers);
            }
        }
    }

    static void AddWord(string text, int start, int end, ImmutableHashSet<string>.Builder all, ImmutableHashSet<string>.Builder declaring, ImmutableHashSet<string>.Builder qualifiers)
    {
        var word = text[start..end];
        all.Add(word);
        while (end < text.Length && text[end] is ' ' or '\t' or '\f' or '\r' or '\n')
        {
            end++;
        }

        if (end == text.Length || text[end] != '.')
        {
            declaring.Add(word);
        }
        else
        {
            qualifiers.Add(word);
        }
    }

    static bool IsWordCharacter(char character) => char.IsAsciiLetterOrDigit(character) || character is '_' or '$';
}
