// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// A line of text: where it starts, how long its content is and how long its line break is.
/// </summary>
/// <param name="Start">The offset of the first character.</param>
/// <param name="Content">The length without the line break.</param>
/// <param name="Break">The length of the line break, zero for the last line.</param>
internal sealed record InlineLine(int Start, int Content, int Break);

/// <summary>
/// Re-indents an authored body to the block it is spliced into. The common leading whitespace is stripped and the block indent is
/// applied; lines that start inside a multi-line literal are never touched, and line breaks are preserved byte for byte.
/// </summary>
internal static class CSharpInlineIndentation
{
    const int SpacesPerLevel = 4;
    const int MaximumLevel = 256;

    enum LineKind
    {
        Code,
        Blank,
        Protected
    }

    /// <summary>
    /// Splits text into lines, keeping each line break (CRLF, LF or CR).
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <returns>The lines, always at least one.</returns>
    internal static List<InlineLine> Lines(string text)
    {
        var lines = new List<InlineLine>();
        var start = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] is not ('\r' or '\n')) continue;
            var length = text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n' ? 2 : 1;
            lines.Add(new(start, index - start, length));
            index += length - 1;
            start = index + 1;
        }

        lines.Add(new(start, text.Length - start, 0));
        return lines;
    }

    /// <summary>
    /// Presents a body at an indentation level, or verbatim when the presentation cannot be verified.
    /// </summary>
    /// <param name="text">The authored body.</param>
    /// <param name="level">The indentation level; each level is four spaces.</param>
    /// <param name="scope">The generated scope the body is spliced into.</param>
    /// <param name="render">Replaces the renderer, so a specification can prove the fallback.</param>
    /// <returns>The re-indented text, or <paramref name="text"/> when verification failed.</returns>
    internal static string Present(string text, int level, InlineCodeScope scope, Func<string, int, string>? render = null)
    {
        var presented = render is null ? Render(text, level, scope) : render(text, level);
        return Verify(text, presented, scope) ? presented : text;
    }

    /// <summary>
    /// Renders a body at an indentation level without verifying it.
    /// </summary>
    /// <param name="text">The authored body.</param>
    /// <param name="level">The indentation level.</param>
    /// <param name="scope">The generated scope the body is spliced into.</param>
    /// <returns>The re-indented text.</returns>
    internal static string Render(string text, int level, InlineCodeScope scope)
    {
        var lines = Lines(text);
        var kinds = Classify(text, lines, scope);
        var common = CommonPrefix(text, lines, kinds);
        var indent = new string(' ', level * SpacesPerLevel);
        var output = new StringBuilder(text.Length + (lines.Count * indent.Length));
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var lineBreak = text.AsSpan(line.Start + line.Content, line.Break);
            switch (kinds[index])
            {
                case LineKind.Protected:
                    output.Append(text.AsSpan(line.Start, line.Content));
                    break;
                case LineKind.Code:
                    output.Append(indent).Append(text.AsSpan(line.Start + common.Length, line.Content - common.Length));
                    break;
            }

            output.Append(lineBreak);
        }

        return output.ToString();
    }

    /// <summary>
    /// Verifies that a presentation only changed leading whitespace outside literals: the same line breaks, untouched protected
    /// lines, the same token sequence and the same containment result.
    /// </summary>
    /// <param name="original">The authored body.</param>
    /// <param name="presented">The re-indented body.</param>
    /// <param name="scope">The generated scope the body is spliced into.</param>
    /// <returns><see langword="true"/> when the presentation is faithful.</returns>
    internal static bool Verify(string original, string presented, InlineCodeScope scope)
    {
        var before = Lines(original);
        var after = Lines(presented);
        if (before.Count != after.Count) return false;
        var kinds = Classify(original, before, scope);
        for (var index = 0; index < before.Count; index++)
        {
            if (!SameBreak(original, before[index], presented, after[index])) return false;
            if (kinds[index] == LineKind.Protected && !SameContent(original, before[index], presented, after[index])) return false;
        }

        var prefix = CSharpInlineLanguagePack.ContextPrefix(scope);
        var suffix = prefix.Length == 0 ? string.Empty : "\n}";
        if (!Tokens(prefix + original + suffix).SequenceEqual(Tokens(prefix + presented + suffix))) return false;
        return scope != InlineCodeScope.MemberBody || SameContainment(original, presented);
    }

    /// <summary>
    /// Finds a body in generated code, however it was presented.
    /// </summary>
    /// <param name="code">The generated code.</param>
    /// <param name="body">The authored body.</param>
    /// <param name="from">The offset to start searching from.</param>
    /// <param name="scope">The generated scope the body was spliced into.</param>
    /// <returns>The location, or <see langword="null"/> when the body is not in the code.</returns>
    internal static InlineBodyLocation? Locate(string code, string body, int from = 0, InlineCodeScope scope = InlineCodeScope.MemberBody)
    {
        from = Math.Clamp(from, 0, code.Length);

        // The presentation indent is a whole number of levels, so only the indents the generated code actually uses can be it.
        var candidates = new List<string> { body };
        foreach (var level in IndentLevels(code, from))
        {
            var rendered = Render(body, level, scope);
            if (!candidates.Contains(rendered, StringComparer.Ordinal)) candidates.Insert(0, rendered);
        }

        InlineBodyLocation? best = null;
        foreach (var candidate in candidates)
        {
            var index = code.IndexOf(candidate, from, StringComparison.Ordinal);
            if (index >= 0 && (best is null || index < best.Start)) best = new(index, candidate, body);
        }

        return best;
    }

    static bool SameBreak(string original, InlineLine before, string presented, InlineLine after) =>
        original.AsSpan(before.Start + before.Content, before.Break).SequenceEqual(presented.AsSpan(after.Start + after.Content, after.Break));

    static bool SameContent(string original, InlineLine before, string presented, InlineLine after) =>
        original.AsSpan(before.Start, before.Content).SequenceEqual(presented.AsSpan(after.Start, after.Content));

    static SortedSet<int> IndentLevels(string code, int from)
    {
        var levels = new SortedSet<int>();
        foreach (var line in Lines(code[from..]))
        {
            var spaces = 0;
            while (spaces < line.Content && code[from + line.Start + spaces] == ' ') spaces++;
            if (spaces % SpacesPerLevel == 0 && spaces / SpacesPerLevel <= MaximumLevel) levels.Add(spaces / SpacesPerLevel);
        }

        return levels;
    }

    static LineKind[] Classify(string text, List<InlineLine> lines, InlineCodeScope scope)
    {
        var prefix = CSharpInlineLanguagePack.ContextPrefix(scope);
        var spans = CSharpInlineLanguagePack.LiteralSpans(text, prefix);
        var kinds = new LineKind[lines.Count];
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var position = line.Start + prefix.Length;
            if (spans.Any(span => span.Start < position && position < span.End)) kinds[index] = LineKind.Protected;
            else if (IsBlank(text.AsSpan(line.Start, line.Content))) kinds[index] = LineKind.Blank;
            else kinds[index] = LineKind.Code;
        }

        return kinds;
    }

    static bool IsBlank(ReadOnlySpan<char> line)
    {
        foreach (var character in line)
        {
            if (!SyntaxFacts.IsWhitespace(character)) return false;
        }

        return true;
    }

    static string CommonPrefix(string text, List<InlineLine> lines, LineKind[] kinds)
    {
        string? common = null;
        for (var index = 0; index < lines.Count; index++)
        {
            if (kinds[index] != LineKind.Code) continue;
            var line = text.AsSpan(lines[index].Start, lines[index].Content);
            var length = 0;
            while (length < line.Length && SyntaxFacts.IsWhitespace(line[length])) length++;
            var leading = line[..length];
            if (common is null)
            {
                common = leading.ToString();
                continue;
            }

            var shared = 0;
            while (shared < common.Length && shared < leading.Length && common[shared] == leading[shared]) shared++;
            common = common[..shared];
        }

        return common ?? string.Empty;
    }

    static (int Kind, string Text)[] Tokens(string source) =>
        [.. SyntaxFactory.ParseTokens(source).Select(token => (token.RawKind, token.Text))];

    static bool SameContainment(string original, string presented)
    {
        var before = CSharpBodyContainment.Analyze(original);
        var after = CSharpBodyContainment.Analyze(presented);
        return before.IsContained == after.IsContained && before.Supported == after.Supported;
    }
}
