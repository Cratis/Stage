// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Scriban.Runtime;

namespace Cratis.Stage.Rendering.Templates;

internal static class TemplateNames
{
    internal static ScriptArray Words(string text)
    {
        var runes = text.EnumerateRunes().ToArray();
        var words = new ScriptArray();
        var word = new StringBuilder();
        for (var index = 0; index < runes.Length; index++)
        {
            var current = runes[index];
            if (!Rune.IsLetterOrDigit(current))
            {
                Flush();
                continue;
            }
            var previous = index > 0 ? runes[index - 1] : default;
            var next = index + 1 < runes.Length ? runes[index + 1] : default;
            if (word.Length > 0 && (Rune.IsDigit(current) != Rune.IsDigit(previous) ||
                (Rune.IsUpper(current) && (Rune.IsLower(previous) || (Rune.IsUpper(previous) && Rune.IsLower(next))))))
            {
                Flush();
            }
            word.Append(current);
        }

        Flush();
        return words;

        void Flush()
        {
            if (word.Length > 0) words.Add(word.ToString());
            word.Clear();
        }
    }

    internal static string Apply(string style, string text)
    {
        var words = Words(text).Cast<string>().Select(word => word.ToLowerInvariant()).ToArray();
        return style switch
        {
            "snake" => string.Join('_', words),
            "kebab" => string.Join('-', words),
            "constant" => string.Join('_', words).ToUpperInvariant(),
            "pascal" => string.Concat(words.Select(Capitalize)),
            "camel" => words.Length == 0 ? string.Empty : words[0] + string.Concat(words.Skip(1).Select(Capitalize)),
            _ => throw JsonScriptValues.Invalid("Unknown naming style.")
        };
    }

    static string Capitalize(string word)
    {
        if (word.Length == 0) return word;
        var first = Rune.GetRuneAt(word, 0);
        return Rune.ToUpperInvariant(first).ToString() + word[first.Utf16SequenceLength..];
    }
}
