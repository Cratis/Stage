// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Translates Unicode escapes once, using JLS 3.3 backslash eligibility rather than recursive replacement.
/// </summary>
public static class JavaUnicodeEscapes
{
    /// <summary>
    /// Decodes eligible escapes, including repeated 'u' characters, without normalizing other text.
    /// </summary>
    /// <param name="text">The raw Java input.</param>
    /// <returns>The translated UTF-16 input.</returns>
    /// <exception cref="LanguagePackRefusal">An eligible escape is malformed.</exception>
    public static string Decode(string text) => DecodeWithMap(text).Text;

    /// <summary>
    /// Decodes eligible escapes like <see cref="Decode"/> and records where each decoded character began in the raw text.
    /// </summary>
    /// <param name="text">The raw Java input.</param>
    /// <returns>The translated text and, for every decoded index, the raw index of the character or escape that produced it.</returns>
    /// <exception cref="LanguagePackRefusal">An eligible escape is malformed.</exception>
    public static (string Text, int[] RawIndexes) DecodeWithMap(string text)
    {
        var result = new StringBuilder(text.Length);
        var map = new List<int>(text.Length);
        var backslashes = 0;
        var previousWasEscape = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            var eligible = previousWasEscape || backslashes % 2 == 0;
            var escaped = character == '\\' && eligible && IsEscapeIntroducer(text, index);
            map.Add(index);
            if (escaped)
            {
                character = ReadEscape(text, ref index);
            }

            result.Append(character);
            backslashes = character == '\\' ? backslashes + 1 : 0;
            previousWasEscape = escaped;
        }

        return (result.ToString(), [.. map]);
    }

    /// <summary>
    /// Detects any eligible backslash followed by 'u', even when its hexadecimal digits are malformed.
    /// </summary>
    /// <param name="text">The raw Java input.</param>
    /// <returns>Whether the text contains a Unicode-escape introducer eligible under JLS 3.3.</returns>
    public static bool ContainsEligibleEscape(string text)
    {
        var backslashes = 0;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            var eligible = character == '\\' && backslashes % 2 == 0;
            if (eligible && IsEscapeIntroducer(text, index)) return true;
            backslashes = character == '\\' ? backslashes + 1 : 0;
        }

        return false;
    }

    static bool IsEscapeIntroducer(string text, int index) => index + 1 < text.Length && text[index + 1] == 'u';

    static char ReadEscape(string text, ref int index)
    {
        var digits = index + 1;
        while (digits < text.Length && text[digits] == 'u') digits++;
        if (text.Length - digits < 4 || !AllHexDigits(text.AsSpan(digits, 4)))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.MalformedUnicodeEscape, "An eligible Java Unicode escape requires four hexadecimal digits.");
        }

        index = digits + 3;
        return (char)ushort.Parse(text.AsSpan(digits, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
    }

    static bool AllHexDigits(ReadOnlySpan<char> digits)
    {
        foreach (var digit in digits)
        {
            if (!char.IsAsciiHexDigit(digit)) return false;
        }

        return true;
    }
}
