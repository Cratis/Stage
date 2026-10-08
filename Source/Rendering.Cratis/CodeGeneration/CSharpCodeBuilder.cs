// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Builds C# source text through a typed, method-based fluent API rather than a text template — every emitted
/// construct is a method call, so the generated shape is driven by code (and verifiable by specs) rather than by
/// string substitution into a template file.
/// </summary>
public class CSharpCodeBuilder
{
    readonly HashSet<string> _usings = new(StringComparer.Ordinal);
    readonly StringBuilder _body = new();
    string? _namespace;
    int _indent;

    /// <summary>
    /// Escapes a value so it can sit inside a C# string literal.
    /// </summary>
    /// <param name="value">The value to escape.</param>
    /// <returns>The escaped text, without the surrounding quotes.</returns>
    public static string Escape(string value)
    {
        var escaped = new StringBuilder();
        foreach (var character in value)
        {
            var category = char.GetUnicodeCategory(character);
            if (char.IsControl(character) || category is UnicodeCategory.Format or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator or UnicodeCategory.Surrogate or UnicodeCategory.OtherNotAssigned)
            {
                escaped.Append($"\\u{(int)character:X4}");
            }
            else
            {
                escaped.Append(character switch
                {
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    _ => character.ToString()
                });
            }
        }

        return escaped.ToString();
    }

    /// <summary>
    /// Renders a value as a quoted C# string literal.
    /// </summary>
    /// <param name="value">The value to render.</param>
    /// <returns>The quoted, escaped literal.</returns>
    public static string StringLiteral(string value) => $"\"{Escape(value)}\"";

    /// <summary>
    /// Adds a <see langword="using"/> directive. Duplicates are ignored; the final output sorts usings alphabetically.
    /// </summary>
    /// <param name="namespace">The namespace to import.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder Using(string @namespace)
    {
        _usings.Add(@namespace);
        return this;
    }

    /// <summary>
    /// Sets the file-scoped namespace.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder Namespace(string @namespace)
    {
        _namespace = @namespace;
        return this;
    }

    /// <summary>
    /// Emits a multiline XML doc <c language="csharp">&lt;summary&gt;</c> at the current indent level.
    /// </summary>
    /// <param name="lines">The summary text, one XML doc line per entry.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder Summary(IEnumerable<string> lines)
    {
        Line("/// <summary>");
        foreach (var line in lines)
        {
            Line($"/// {line}");
        }

        Line("/// </summary>");
        return this;
    }

    /// <summary>
    /// Emits a single-line multiline XML doc <c language="csharp">&lt;summary&gt;</c> at the current indent level.
    /// </summary>
    /// <param name="text">The summary text.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder Summary(string text) => Summary([text]);

    /// <summary>
    /// Emits authoring metadata as escaped XML documentation, preserving a legacy summary when metadata is absent.
    /// </summary>
    /// <param name="description">The optional authored summary.</param>
    /// <param name="documentation">The optional Markdown remarks, copied as text.</param>
    /// <param name="fallbackSummary">The existing generated summary, if any.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder Documentation(string? description, string? documentation = null, string? fallbackSummary = null)
    {
        if (description is not null)
        {
            TextDocumentation("summary", description);
        }
        else if (fallbackSummary is not null)
        {
            Summary(fallbackSummary);
        }

        if (documentation is not null)
        {
            TextDocumentation("remarks", documentation);
        }

        return this;
    }

    /// <summary>
    /// Emits an attribute usage at the current indent level.
    /// </summary>
    /// <param name="attribute">The attribute content, without the surrounding brackets (e.g. <c language="csharp">Command</c>).</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder Attribute(string attribute) => Line($"[{attribute}]");

    /// <summary>
    /// Emits a raw line of source text at the current indent level.
    /// </summary>
    /// <param name="text">The line to emit; an empty string emits a blank line.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder Line(string text = "")
    {
        if (text.Length == 0)
        {
            _body.AppendLine();
        }
        else
        {
            _body.Append(' ', _indent * 4).AppendLine(text);
        }

        return this;
    }

    /// <summary>
    /// Emits a blank line.
    /// </summary>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder BlankLine() => Line();

    /// <summary>
    /// Embeds pre-formatted, multiline source text verbatim, indenting every line to the current block level while
    /// preserving each line's own relative indentation — used to splice authored code blocks (Screenplay
    /// <c language="csharp">csharp</c> blocks) into generated method bodies.
    /// </summary>
    /// <param name="text">The raw source text to embed.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder Raw(string text)
    {
        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            Line(line);
        }

        return this;
    }

    /// <summary>
    /// Embeds authored source bytes without normalizing line endings or indentation.
    /// </summary>
    /// <param name="text">The authored body.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder RawVerbatim(string text)
    {
        _body.Append(text);
        if (!text.EndsWith('\n'))
        {
            _body.AppendLine();
        }

        return this;
    }

    /// <summary>
    /// Emits an expression-bodied member: <c language="csharp">&lt;signature&gt; =&gt; &lt;expression&gt;;</c>.
    /// </summary>
    /// <param name="signature">The member signature, without a trailing <c language="csharp">=&gt;</c>.</param>
    /// <param name="expression">The expression body.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder ExpressionMember(string signature, string expression) => Line($"{signature} => {expression};");

    /// <summary>
    /// Opens a braced block — a type, method, or control-flow construct — emitting its signature followed by
    /// <c language="csharp">{</c>, and indenting every subsequent line until the matching <see cref="EndBlock"/>.
    /// </summary>
    /// <param name="signature">The block's signature line.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder OpenBlock(string signature)
    {
        Line(signature);
        Line("{");
        _indent++;
        return this;
    }

    /// <summary>
    /// Closes the most recently opened block.
    /// </summary>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder EndBlock()
    {
        _indent--;
        Line("}");
        return this;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var result = new StringBuilder()
            .AppendLine("// Copyright (c) Cratis. All rights reserved.")
            .AppendLine("// Licensed under the MIT license. See LICENSE file in the project root for full license information.")
            .AppendLine();

        foreach (var @using in _usings.Order(StringComparer.Ordinal))
        {
            result.AppendLine($"using {@using};");
        }

        if (_usings.Count > 0)
        {
            result.AppendLine();
        }

        if (_namespace is not null)
        {
            result.AppendLine($"namespace {_namespace};").AppendLine();
        }

        result.Append(_body);
        return result.ToString();
    }

    internal static string NormalizeDocumentation(string text) => string.Join(
        '\n',
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
            .Replace('\u0085', '\n').Replace('\u2028', '\n').Replace('\u2029', '\n')
            .Split('\n').Select(line => line.TrimEnd())).TrimEnd();

    void TextDocumentation(string tag, string text)
    {
        Line($"/// <{tag}>");
        foreach (var line in NormalizeDocumentation(text).Split('\n'))
        {
            var escaped = line.Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal);
            Line(escaped.Length == 0 ? "///" : $"/// {escaped}");
        }

        Line($"/// </{tag}>");
    }
}
