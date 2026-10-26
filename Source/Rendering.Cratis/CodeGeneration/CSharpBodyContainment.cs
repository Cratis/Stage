// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

internal sealed record CSharpContainmentProof(BlockSyntax? Block, bool Supported, string? Reason)
{
    internal bool IsContained => Block is not null;
    internal bool HasDirectives { get; init; }
}

internal static class CSharpBodyContainment
{
    internal static CSharpContainmentProof Analyze(string source)
    {
        var text = "{\n" + source + "\n}";
        var parsed = SyntaxFactory.ParseStatement(text, consumeFullText: true);
        var directives = parsed.DescendantTrivia(descendIntoTrivia: true).Where(trivia => trivia.IsDirective).ToArray();
        if (directives.Length == 0) return Prove(text);
        if (!HasBoundary(parsed, text)) return new(null, false, "Directives consume the generated member boundary.") { HasDirectives = true };

        // Inactive branches must not hide escapes or swallow the generated terminator.
        // Remove directive lines only for proof; no altered authored source is ever emitted.
        var proof = Prove(WithoutDirectives(text, directives));
        return (proof.IsContained ? new(proof.Block, false, "Directives are not supported in inline bodies.") : proof) with { HasDirectives = true };
    }

    static CSharpContainmentProof Prove(string text)
    {
        // Token diagnostics cover unterminated strings/comments even when newline recovery leaves a closing brace.
        if (SyntaxFactory.ParseTokens(text).Any(token => token.ContainsDiagnostics || token.IsKind(SyntaxKind.BadToken)))
        {
            return new(null, false, "Lexical containment cannot be proved.");
        }

        var parsed = SyntaxFactory.ParseStatement(text, consumeFullText: true);
        if (HasUnprovenLiteral(parsed)) return new(null, false, "Lexical containment cannot be proved.");
        if (!HasBoundary(parsed, text) || parsed.ContainsSkippedText)
        {
            return new(null, false, "The authored body escapes or consumes its generated member boundary.");
        }

        return new((BlockSyntax)parsed, !parsed.ContainsDiagnostics, parsed.ContainsDiagnostics ? "The contained C# syntax is unsupported." : null);
    }

    // Raw/interpolated string diagnostics can be attached only during parsing, not standalone tokenization.
    static bool HasUnprovenLiteral(StatementSyntax parsed) => parsed.DescendantNodes().Any(HasLiteralDiagnostic);

    static bool HasLiteralDiagnostic(SyntaxNode node)
    {
        if (node is not LiteralExpressionSyntax and not InterpolatedStringExpressionSyntax) return false;
        return node.ContainsDiagnostics || node.DescendantTokens().Any(token => token.IsMissing);
    }

    static bool HasBoundary(StatementSyntax parsed, string text)
    {
        if (parsed is not BlockSyntax block || block.CloseBraceToken.IsMissing) return false;
        return block.OpenBraceToken.SpanStart == 0 && block.CloseBraceToken.SpanStart == text.Length - 1;
    }

    static string WithoutDirectives(string text, SyntaxTrivia[] directives)
    {
        var active = text.ToCharArray();
        foreach (var directive in directives)
        {
            for (var index = directive.SpanStart; index < directive.Span.End; index++)
            {
                if (active[index] is not '\r' and not '\n') active[index] = ' ';
            }
        }

        return new(active);
    }
}
