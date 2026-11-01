// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

internal sealed record InlineCodeSplice(string? Text, ImmutableArray<TextSpan> Authored, ImmutableArray<InlineCodeDiagnostic> Diagnostics);

internal static class InlineCodeSplicer
{
    internal static InlineCodeSplice Splice(string skeleton, InlineCodeTokens registry, string? issuedSkeleton = null)
    {
        var diagnostics = ImmutableArray.CreateBuilder<InlineCodeDiagnostic>();
        var occurrences = Find(skeleton, registry, diagnostics);
        foreach (var spelling in registry.Slots.Keys)
        {
            if (occurrences.Count(occurrence => occurrence.Spelling == spelling) != 1)
            {
                diagnostics.Add(new("STAGE-INLINE-INTEGRITY", "An issued authenticated placement must occur exactly once."));
            }
        }

        if (diagnostics.Count == 0 && !SameSites(issuedSkeleton ?? skeleton, skeleton, registry))
        {
            diagnostics.Add(new("STAGE-INLINE-INTEGRITY", "Finish changed an authenticated placement's generated scope or token order."));
        }

        if (diagnostics.Count > 0) return new(null, [], diagnostics.ToImmutable());
        return ReplacePlacements(skeleton, registry, occurrences);
    }

    static InlineCodeSplice ReplacePlacements(string skeleton, InlineCodeTokens registry, List<(int Start, string Spelling)> occurrences)
    {
        var output = new StringBuilder();
        var authored = ImmutableArray.CreateBuilder<TextSpan>();
        var copied = 0;
        foreach (var (start, spelling) in occurrences)
        {
            output.Append(skeleton.AsSpan(copied, start - copied));
            var slot = registry.Slots[spelling];
            var mapped = slot.Source is not null && IsMappable(output, slot, slot.Source);
            if (mapped) output.Append($"#line {slot.Source!.Line} \"{slot.Source.Path}\"\n");
            if (slot.IsAuthored) authored.Add(new(output.Length, slot.Presentation.Length));
            output.Append(slot.Presentation);
            if (mapped) output.Append("#line default\n");
            copied = start + spelling.Length;
        }

        output.Append(skeleton.AsSpan(copied));
        return new(output.ToString(), authored.ToImmutable(), []);
    }

    static bool IsMappable(StringBuilder output, InlineCodeSlot slot, InlineSourceLine source) =>
        slot.IsAuthored
        && (output.Length == 0 || output[^1] == (char)10)
        && slot.Presentation.EndsWith((char)10)
        && slot.Presentation.Count(character => character == (char)10) == source.LineCount;

    static bool SameSites(string original, string finished, InlineCodeTokens registry)
    {
        var before = Sites(original, registry).ToArray();
        var after = Sites(finished, registry).ToArray();
        return before.Length == registry.Slots.Count && before.SequenceEqual(after);
    }

    static IEnumerable<string> Sites(string skeleton, InlineCodeTokens registry)
    {
        var root = CSharpSyntaxTree.ParseText(skeleton).GetRoot();
        var tokens = SyntaxFactory.ParseTokens(skeleton).ToArray();
        var comments = tokens.SelectMany(token => token.LeadingTrivia.Concat(token.TrailingTrivia))
            .Where(trivia => registry.Slots.ContainsKey(trivia.ToString())).OrderBy(trivia => trivia.SpanStart);
        foreach (var comment in comments)
        {
            yield return JsonSerializer.Serialize(new
            {
                Token = comment.ToString(),
                Scopes = ScopesBefore(root, tokens, comment.SpanStart),
                Namespaces = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>()
                    .Where(scope => scope.FullSpan.Contains(comment.Span)).Select(SyntaxIdentity),
                Before = TokenIdentity(tokens.LastOrDefault(token => token.Span.End <= comment.SpanStart)),
                After = TokenIdentity(tokens.FirstOrDefault(token => token.SpanStart >= comment.Span.End))
            });
        }
    }

    static IEnumerable<string> ScopesBefore(SyntaxNode root, SyntaxToken[] tokens, int position)
    {
        var path = new Stack<string>();
        var opened = 0;
        foreach (var token in tokens.TakeWhile(token => token.SpanStart < position))
        {
            // Lexical braces exclude generated literals; syntax headers bind scopes to their actual owners.
            if (token.IsKind(SyntaxKind.OpenBraceToken))
            {
                var scope = root.FindToken(token.SpanStart, findInsideTrivia: true).Parent!;
                path.Push(JsonSerializer.Serialize(new
                {
                    Ordinal = ++opened,
                    Identity = scope.AncestorsAndSelf().Reverse().Where(node => node is not CompilationUnitSyntax).Select(SyntaxIdentity)
                }));
            }

            if (token.IsKind(SyntaxKind.CloseBraceToken) && path.Count > 0) path.Pop();
        }

        return path.Reverse();
    }

    static string SyntaxIdentity(SyntaxNode node) => JsonSerializer.Serialize(new
    {
        node.RawKind,
        Header = node.ChildNodesAndTokens().TakeWhile(part => !IsBodyBoundary(part))
            .SelectMany(part => part.IsToken ? [part.AsToken()] : part.AsNode()!.DescendantTokens()).Select(TokenIdentity)
    });

    static bool IsBodyBoundary(SyntaxNodeOrToken part) => part.IsToken
        ? part.IsKind(SyntaxKind.OpenBraceToken) || part.IsKind(SyntaxKind.CloseBraceToken) || part.IsKind(SyntaxKind.SemicolonToken)
        : part.AsNode() is StatementSyntax or MemberDeclarationSyntax or AccessorListSyntax or ArrowExpressionClauseSyntax or EqualsValueClauseSyntax;

    static string TokenIdentity(SyntaxToken token) => JsonSerializer.Serialize(new { token.RawKind, token.Text });

    static List<(int Start, string Spelling)> Find(string skeleton, InlineCodeTokens registry, ImmutableArray<InlineCodeDiagnostic>.Builder diagnostics)
    {
        var occurrences = new List<(int, string)>();
        var cursor = 0;
        while (skeleton.IndexOf(InlineCodeTokens.Prefix, cursor, StringComparison.Ordinal) is var start && start >= 0)
        {
            var end = skeleton.IndexOf(InlineCodeTokens.Suffix, start + InlineCodeTokens.Prefix.Length, StringComparison.Ordinal);
            if (end < 0)
            {
                diagnostics.Add(new("STAGE-INLINE-INTEGRITY", "An inline token is unterminated."));
                break;
            }

            var spelling = skeleton[start..(end + InlineCodeTokens.Suffix.Length)];
            if (!registry.Slots.ContainsKey(spelling)) diagnostics.Add(new("STAGE-INLINE-INTEGRITY", "An inline token is unauthenticated or belongs to another render."));
            occurrences.Add((start, spelling));
            cursor = end + InlineCodeTokens.Suffix.Length;
        }

        return occurrences;
    }
}
