// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Handles the controls and Unicode format characters Java ignores inside identifiers.
/// </summary>
static class JavaIdentifierIgnorables
{
    internal static string Remove(string text)
    {
        var result = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            result.Append(IsIgnorable(rune) ? string.Empty : rune.ToString());
        }

        return result.ToString();
    }

    static bool IsIgnorable(Rune rune) => rune.Value is (>= 0 and <= 8) or (>= 14 and <= 27) or (>= 127 and <= 159) || Rune.GetUnicodeCategory(rune) == UnicodeCategory.Format;
}
