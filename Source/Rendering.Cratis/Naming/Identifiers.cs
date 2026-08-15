// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Converts Screenplay names into valid, idiomatically-cased C# identifiers.
/// </summary>
public static class Identifiers
{
    static readonly char[] _separators = [' ', '_', '-', '.'];

    static readonly HashSet<string> _reservedKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
        "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
    };

    /// <summary>
    /// Converts a name into PascalCase.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The PascalCase identifier.</returns>
    public static string ToPascalCase(string name)
    {
        var result = string.Concat(SplitWords(name).Select(CapitalizeFirst));
        if (result.Length == 0)
        {
            return "Item";
        }

        return char.IsDigit(result[0]) ? $"_{result}" : result;
    }

    /// <summary>
    /// Converts a name into camelCase.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The camelCase identifier.</returns>
    public static string ToCamelCase(string name)
    {
        var pascal = ToPascalCase(name);
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    /// <summary>
    /// Converts a name into lowercase, space-separated words — used to synthesize doc-comment text for
    /// constructs (like Screenplay events) that carry no authored description.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The lowercase, space-separated words.</returns>
    public static string ToWords(string name) => string.Join(' ', SplitWords(name).SelectMany(SplitOnCaseBoundary)).ToLowerInvariant();

    /// <summary>
    /// Converts a name into snake_case — used for the spec folder and class names the repository conventions
    /// use, where <c>RegisteringADraftInvoice</c> reads as <c>registering_a_draft_invoice</c>.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The snake_case name.</returns>
    public static string ToSnakeCase(string name) => ToWords(name).Replace(' ', '_');

    /// <summary>
    /// Escapes an identifier with <c>@</c> when it is a reserved C# keyword.
    /// </summary>
    /// <param name="identifier">The identifier to escape.</param>
    /// <returns>The escaped identifier, or the original identifier when escaping is not needed.</returns>
    public static string EscapeKeyword(string identifier) =>
        _reservedKeywords.Contains(identifier) ? $"@{identifier}" : identifier;

    static string[] SplitWords(string name) => name.Split(_separators, StringSplitOptions.RemoveEmptyEntries);

    static string CapitalizeFirst(string word) => char.ToUpperInvariant(word[0]) + word[1..];

    static IEnumerable<string> SplitOnCaseBoundary(string word)
    {
        var start = 0;
        for (var i = 1; i < word.Length; i++)
        {
            if (char.IsUpper(word[i]) && !char.IsUpper(word[i - 1]))
            {
                yield return word[start..i];
                start = i;
            }
        }

        yield return word[start..];
    }
}
