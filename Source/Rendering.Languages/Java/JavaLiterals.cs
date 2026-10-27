// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Escapes values for Java literals and line-comment text without introducing eligible Unicode escapes from input backslashes.
/// </summary>
public static class JavaLiterals
{
    /// <summary>
    /// Escapes a string and encloses it in double quotes.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The Java string literal.</returns>
    public static string EscapeString(string value) => "\"" + string.Concat(value.Select(character => Escape(character, '"'))) + "\"";

    /// <summary>
    /// Escapes one UTF-16 code unit and encloses it in single quotes.
    /// </summary>
    /// <param name="value">The character value.</param>
    /// <returns>The Java character literal.</returns>
    public static string EscapeCharacter(char value) => "'" + Escape(value, '\'') + "'";

    /// <summary>
    /// Escapes line-comment content by doubling backslashes and replacing line breaks and controls with spaces.
    /// </summary>
    /// <param name="value">The comment text, without the line-comment prefix.</param>
    /// <returns>Text that cannot terminate a Java line comment, even during Unicode translation.</returns>
    public static string EscapeLineComment(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            result.Append(character switch
            {
                '\\' => "\\\\",
                '\r' or '\n' or '\u2028' or '\u2029' or '\u0085' => " ",
                _ when character < 0x20 => " ",
                _ => character.ToString(),
            });
        }

        return result.ToString();
    }

    static string Escape(char character, char quote)
    {
        if (character == '\\' || character == quote) return "\\" + character;
        if (character < 0x20) return EscapeControl(character);
        return character > 0x7E ? EscapeUnicode(character) : character.ToString();
    }

    static string EscapeControl(char character) => character switch
    {
        '\b' => "\\b",
        '\t' => "\\t",
        '\n' => "\\n",
        '\f' => "\\f",
        '\r' => "\\r",
        _ => EscapeUnicode(character),
    };

    static string EscapeUnicode(char character) => "\\u" + ((int)character).ToString("x4", CultureInfo.InvariantCulture);
}
