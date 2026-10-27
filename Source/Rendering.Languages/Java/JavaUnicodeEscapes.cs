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
    public static string Decode(string text) => Translate(text, out _);

    /// <summary>
    /// Determines whether the final raw backslash could start an escape when more output is appended.
    /// Eligibility includes backslashes produced by earlier Unicode escapes, matching javac's UnicodeReader.
    /// </summary>
    /// <param name="text">The raw Java input.</param>
    /// <returns>Whether a continuation starting with 'u' could extend the final backslash into an escape.</returns>
    /// <exception cref="LanguagePackRefusal">An eligible escape is malformed.</exception>
    public static bool EndsWithEligibleBackslash(string text)
    {
        Translate(text, out var endsWithEligibleBackslash);
        return endsWithEligibleBackslash;
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

    static string Translate(string text, out bool endsWithEligibleBackslash)
    {
        var result = new StringBuilder(text.Length);
        var backslashes = 0;
        var previousWasEscape = false;
        endsWithEligibleBackslash = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            var eligible = previousWasEscape || backslashes % 2 == 0;
            var eligibleBackslash = character == '\\' && eligible;
            var escaped = eligibleBackslash && IsEscapeIntroducer(text, index);
            endsWithEligibleBackslash = eligibleBackslash && !escaped;
            if (escaped)
            {
                character = ReadEscape(text, ref index);
            }

            result.Append(character);
            backslashes = character == '\\' ? backslashes + 1 : 0;
            previousWasEscape = escaped;
        }

        return result.ToString();
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
