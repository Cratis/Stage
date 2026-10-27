// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using Scriban.Functions;
using Scriban.Runtime;

namespace Cratis.Stage.Rendering.Templates;

internal static class LockedBuiltins
{
    // Audited against 7.5.3: no ambient time, random, eval, JSON, regex, encoding or culture selectors.
    internal static readonly IReadOnlyDictionary<string, string[]> Members = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["string"] = ["append", "prepend", "capitalize", "capitalizewords", "contains", "downcase", "upcase", "empty", "whitespace", "ends_with", "starts_with", "equals_ignore_case", "handleize", "literal", "escape", "lstrip", "rstrip", "strip", "pluralize", "remove", "remove_first", "remove_last", "replace", "replace_first", "size", "slice", "slice1", "split", "to_int", "to_long", "to_float", "to_double", "truncate", "pad_left", "pad_right", "index_of"],
        ["array"] = ["add", "add_range", "compact", "concat", "cycle", "any", "each", "filter", "first", "join", "last", "limit", "map", "offset", "remove_at", "reverse", "size", "sort", "uniq", "contains"],
        ["math"] = ["abs", "ceil", "divided_by", "floor", "is_number", "minus", "modulo", "plus", "round", "times"],
        ["object"] = ["default", "has_key", "has_value", "keys", "size", "typeof", "kind", "values"]
    };

    internal static ScriptObject Create()
    {
        var root = new ScriptObject();
        foreach (var (name, members) in Members)
        {
            ScriptObject functions = name switch
            {
                "string" => new StringFunctions(),
                "array" => new ArrayFunctions(),
                "math" => new MathFunctions(),
                _ => new ObjectFunctions()
            };
            if (members.Any(member => !functions.ContainsKey(member))) throw new InvalidOperationException("Pinned builtin allowlist contains an unknown member.");
            foreach (var key in functions.Keys.ToArray())
            {
                if (!members.Contains(key, StringComparer.Ordinal)) functions.Remove(key);
            }

            if (name == "string") ReplaceStrings(functions);
            if (name == "array") Replace(functions, "sort", new Func<object?, string?, ScriptArray>(Sort));
            functions.IsReadOnly = true;
            root.SetValue(name, functions, true);
        }

        root.SetValue("empty", EmptyScriptObject.Default, true);
        root.SetValue("blank", EmptyScriptObject.Default, true);
        return root;
    }

    static void ReplaceStrings(ScriptObject functions)
    {
        Replace(functions, "index_of", new Func<string?, string?, int?, int?, string?, int>(IndexOf));
        Replace(functions, "starts_with", new Func<string?, string?, bool>((text, value) => text is not null && value is not null && text.StartsWith(value, StringComparison.Ordinal)));
        Replace(functions, "ends_with", new Func<string?, string?, bool>((text, value) => text is not null && value is not null && text.EndsWith(value, StringComparison.Ordinal)));
        Replace(functions, "equals_ignore_case", new Func<string?, string?, bool>((text, value) => string.Equals(text, value, StringComparison.OrdinalIgnoreCase)));
        Replace(functions, "capitalize", new Func<string?, string?>(text => string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..]));
        Replace(functions, "capitalizewords", new Func<string?, string?>(CapitalizeWords));
    }

    static void Replace(ScriptObject functions, string name, Delegate implementation)
    {
        functions.Remove(name);
        functions.Import(name, implementation);
    }

    static int IndexOf(string? text, string? search, int? start = null, int? count = null, string? comparison = null)
    {
        if (comparison is not null && comparison != "Ordinal") throw new TemplateRefusal(new(TemplateCodes.Input, "Only ordinal comparison is supported.", string.Empty, 0, 0));
        if (text is null || search is null) return -1;
        var offset = Math.Clamp(start ?? 0, 0, text.Length);
        return text.IndexOf(search, offset, Math.Clamp(count ?? (text.Length - offset), 0, text.Length - offset), StringComparison.Ordinal);
    }

    static string? CapitalizeWords(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var chars = text.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            if (char.IsLetter(chars[index]) && (index == 0 || char.IsWhiteSpace(chars[index - 1]))) chars[index] = char.ToUpperInvariant(chars[index]);
        }

        return new(chars);
    }

    static ScriptArray Sort(object? value, string? member = null)
    {
        if (value is not IEnumerable list || value is string) throw new TemplateRefusal(new(TemplateCodes.Input, "array.sort requires an array.", string.Empty, 0, 0));
        object? Key(object? item)
        {
            if (member is null) return item;
            return item is ScriptObject data ? data[member] : null;
        }
        var sorted = new ScriptArray();
        foreach (var item in list.Cast<object?>().OrderBy(Key, Comparer<object?>.Create(Compare))) sorted.Add(item);
        return sorted;
    }

    static int Compare(object? left, object? right)
    {
        var rank = Rank(left).CompareTo(Rank(right));
        if (rank != 0) return rank;
        return left switch
        {
            null => 0,
            string text => string.CompareOrdinal(text, (string)right!),
            bool boolean => boolean.CompareTo((bool)right!),
            _ when Rank(left) == 2 => Convert.ToDecimal(left, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(right, CultureInfo.InvariantCulture)),
            _ => throw new TemplateRefusal(new(TemplateCodes.Input, "array.sort keys must be primitive JSON values.", string.Empty, 0, 0))
        };
    }

    static int Rank(object? value) => value switch { null => 0, bool => 1, int or long or decimal or double => 2, string => 3, _ => 4 };
}
