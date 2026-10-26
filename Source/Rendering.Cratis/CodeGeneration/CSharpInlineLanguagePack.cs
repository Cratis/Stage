// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

internal enum InlineCodeScope
{
    MemberBody,
    TypeMembers,
    CompilationUnit
}

internal sealed record InlineCodePresentation(string? Text, bool Degraded, ImmutableArray<InlineCodeDiagnostic> Diagnostics);
internal sealed record InlineCodePresentationOptions(int Indent, bool Verbatim, InlineCodeScope Scope = InlineCodeScope.MemberBody);

internal static class CSharpInlineLanguagePack
{
    internal const string Language = "csharp";
    static readonly HashSet<string> _roles = new(["command", "operation", "generation", "reaction", "guard", "validation", "builder"], StringComparer.Ordinal);

    internal static InlineCodePresentation Present(InlineCode code, int indent = 0, bool verbatim = false)
    {
        if (indent is < 0 or > 256) return Refuse("The inline indentation is invalid.");
        if (!_roles.Contains(code.Placement.Role)) return Refuse("The inline role is unknown.");

        // Without a foreign language parser, only an empty/whitespace body has a language-independent proof.
        if (!HasLanguageProof(code)) return Refuse("No structural containment proof is available for this foreign language body.");
        return PresentProven(code, new(indent, verbatim));
    }

    internal static InlineCodePresentation PresentLegacy(InlineCode code, InlineCodePresentationOptions options)
    {
        if (options.Scope == InlineCodeScope.MemberBody) return Present(code, options.Indent, options.Verbatim);
        if (!CSharpBodyContainment.DeclarationsAreContained(code)) return Refuse("The authored declarations escape their scope or have unprovable lexical containment.");
        return PresentText(code, options);
    }

    internal static string Fingerprint(IEnumerable<InlineCode> inputs)
    {
        var entries = inputs.Select(code => JsonSerializer.Serialize(new[]
        {
            code.RequirementId, code.Placement.Role, code.Placement.Owner, code.Placement.Member,
            code.Language, code.ContentHash, code.ByteSource
        })).Order(StringComparer.Ordinal);
        var canonical = "stage-inline-v1\n" + string.Join('\n', entries);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    static bool LanguageMatches(InlineCode code) => string.Equals(code.Language, Language, StringComparison.Ordinal);
    static bool HasLanguageProof(InlineCode code) => LanguageMatches(code) || code.Text.All(char.IsWhiteSpace);

    static InlineCodePresentation PresentProven(InlineCode code, InlineCodePresentationOptions options)
    {
        var proof = CSharpBodyContainment.Analyze(code.Text);
        if (!proof.IsContained) return Refuse(proof.Reason!);
        if (!LanguageMatches(code) || !proof.Supported) return Placeholder(code, proof);
        return PresentText(code, options);
    }

    static InlineCodePresentation PresentText(InlineCode code, InlineCodePresentationOptions options) =>
        new(options.Verbatim ? Terminate(code.Text) : Indent(code, options), false, []);

    static InlineCodePresentation Placeholder(InlineCode code, CSharpContainmentProof proof)
    {
        if (string.Equals(code.Placement.Role, "guard", StringComparison.Ordinal) || string.Equals(code.Placement.Role, "validation", StringComparison.Ordinal)) return Refuse("The enclosing operation must be denied before effects; no body-only guard placeholder is safe.");
        return new("throw new global::System.NotSupportedException(\"STAGE-DEGRADE-003\");\n", true, [new("STAGE-DEGRADE-003", proof.Reason ?? "The body language does not match csharp.")]);
    }

    static InlineCodePresentation Refuse(string message) => new(null, false, [new("STAGE-INLINE-INTEGRITY", message)]);
    static string Terminate(string text) => text.EndsWith('\n') ? text : text + "\n";

    static string Indent(InlineCode code, InlineCodePresentationOptions options)
    {
        var text = code.Text;
        var prefix = ContextPrefix(options);
        var tree = CSharpSyntaxTree.ParseText(prefix + text + (prefix.Length == 0 ? string.Empty : "\n}"));
        var root = tree.GetRoot();
        var spans = LiteralSpans(root);
        var source = SourceText.From(text);
        var output = new StringBuilder();
        foreach (var line in source.Lines)
        {
            var position = line.Start + prefix.Length;
            var protectedLine = spans.Any(span => span.Start < position + line.SpanIncludingLineBreak.Length && span.End > position);
            if (!protectedLine && line.Span.Length > 0) output.Append(' ', options.Indent * 4);
            output.Append(text.AsSpan(line.Start, line.SpanIncludingLineBreak.Length));
        }

        return Terminate(output.ToString());
    }

    static string ContextPrefix(InlineCodePresentationOptions options) => options.Scope switch
    {
        InlineCodeScope.MemberBody => "{\n",
        InlineCodeScope.TypeMembers => "class __StageInlineHost {\n",
        _ => string.Empty
    };

    static TextSpan[] LiteralSpans(SyntaxNode root) =>
        [.. root.DescendantTokens().Where(token => token.Text.Contains('\n') || token.Text.Contains('\r')).Select(token => token.Span),
            .. root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>()
                .Where(expression => expression.ToString().Contains('\n') || expression.ToString().Contains('\r')).Select(expression => expression.Span)];
}
