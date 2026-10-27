// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Stage.Rendering.Templates;

internal static class TemplatePreScan
{
    internal const int MaxBytes = 65536;

    internal static void Check(string path, string text)
    {
        if (Encoding.UTF8.GetByteCount(text) > MaxBytes)
        {
            throw new TemplateRefusal(new(TemplateCodes.TemplateSize, "Template exceeds 65536 UTF-8 bytes.", path, 0, 0));
        }

        CheckPrivateUse(path, text);
    }

    internal static bool HasPrivateUse(string text) => text.EnumerateRunes().Any(rune => Rune.GetUnicodeCategory(rune) == UnicodeCategory.PrivateUse);

    static void CheckPrivateUse(string path, string text)
    {
        var line = 1;
        var column = 1;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.PrivateUse)
            {
                throw new TemplateRefusal(new(TemplateCodes.TemplatePrivateUse, "Private-use code point in template.", path, line, column));
            }

            if (rune.Value == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column += rune.Utf16SequenceLength;
            }
        }
    }
}
