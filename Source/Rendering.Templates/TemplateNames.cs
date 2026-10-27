// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Scriban.Runtime;

namespace Cratis.Stage.Rendering.Templates;

internal static class TemplateNames
{
    internal static string Apply(string style, object? value)
    {
        if (value is not ScriptArray { Count: > 0 } array || array.Any(word => word is not string { Length: > 0 })) throw JsonScriptValues.Invalid("Naming requires a nonempty array of nonempty words.");
        var words = array.Cast<string>().ToArray();
        return style switch
        {
            "pascal" => string.Concat(words.Select(Capitalize)),
            "camel" => Lower(words[0]) + string.Concat(words.Skip(1).Select(Capitalize)),
            "snake" => string.Join('_', words.Select(Lower)),
            "kebab" => string.Join('-', words.Select(Lower)),
            "constant" => string.Join('_', words.Select(word => Case(word, true))),
            "words" => string.Join(' ', words.Select(Lower)),
            _ => throw JsonScriptValues.Invalid("Unknown naming style.")
        };
    }

    static string Lower(string word) => Case(word, false);

    static string Case(string word, bool upper) => string.Concat(word.EnumerateRunes().Select(rune => (upper ? Rune.ToUpperInvariant(rune) : Rune.ToLowerInvariant(rune)).ToString()));

    static string Capitalize(string word)
    {
        var first = Rune.GetRuneAt(word, 0);
        return Rune.ToUpperInvariant(first).ToString() + word[first.Utf16SequenceLength..];
    }
}
