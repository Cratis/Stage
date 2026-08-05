// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// Emits a multiline XML doc <c>&lt;summary&gt;</c> at the current indent level.
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
    /// Emits a single-line multiline XML doc <c>&lt;summary&gt;</c> at the current indent level.
    /// </summary>
    /// <param name="text">The summary text.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder Summary(string text) => Summary([text]);

    /// <summary>
    /// Emits an attribute usage at the current indent level.
    /// </summary>
    /// <param name="attribute">The attribute content, without the surrounding brackets (e.g. <c>Command</c>).</param>
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
    /// <c>csharp</c> blocks) into generated method bodies.
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
    /// Emits an expression-bodied member: <c>&lt;signature&gt; =&gt; &lt;expression&gt;;</c>.
    /// </summary>
    /// <param name="signature">The member signature, without a trailing <c>=&gt;</c>.</param>
    /// <param name="expression">The expression body.</param>
    /// <returns>The builder, for chaining.</returns>
    public CSharpCodeBuilder ExpressionMember(string signature, string expression) => Line($"{signature} => {expression};");

    /// <summary>
    /// Opens a braced block — a type, method, or control-flow construct — emitting its signature followed by
    /// <c>{</c>, and indenting every subsequent line until the matching <see cref="EndBlock"/>.
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
}
