// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Rejects private-use scalar values and ill-formed UTF-16, both raw and after one JLS 3.3 translation.
/// </summary>
public static class PrivateUseCodePoints
{
    /// <summary>
    /// Validates raw text and its Unicode-translated interpretation without rewriting the original text.
    /// </summary>
    /// <param name="text">The input field.</param>
    /// <exception cref="LanguagePackRefusal">The field contains private-use values, lone surrogates, or malformed eligible escapes.</exception>
    public static void Validate(string text)
    {
        ValidateRaw(text);
        ValidateRaw(JavaUnicodeEscapes.Decode(text));
    }

    internal static void ValidateRaw(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            var codePoint = ReadCodePoint(text, ref index);
            if (codePoint is (>= 0xE000 and <= 0xF8FF) or (>= 0xF0000 and <= 0xFFFFD) or (>= 0x100000 and <= 0x10FFFD))
            {
                throw new LanguagePackRefusal(JavaRefusalCodes.PrivateUseCodePoint, "Java symbol input must not contain private-use code points.");
            }
        }
    }

    static int ReadCodePoint(string text, ref int index)
    {
        if (!Rune.TryGetRuneAt(text, index, out var rune))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.LoneSurrogate, "Java symbol input must not contain lone UTF-16 surrogates.");
        }

        index += rune.Utf16SequenceLength - 1;
        return rune.Value;
    }
}
