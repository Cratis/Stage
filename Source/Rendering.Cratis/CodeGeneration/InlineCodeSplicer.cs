// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
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
        var output = new StringBuilder();
        var authored = ImmutableArray.CreateBuilder<TextSpan>();
        var copied = 0;
        foreach (var (start, spelling) in occurrences)
        {
            output.Append(skeleton.AsSpan(copied, start - copied));
            var presentation = registry.Slots[spelling].Presentation;
            authored.Add(new(output.Length, presentation.Length));
            output.Append(presentation);
            copied = start + spelling.Length;
        }

        output.Append(skeleton.AsSpan(copied));
        return new(output.ToString(), authored.ToImmutable(), []);
    }

    static bool SameSites(string original, string finished, InlineCodeTokens registry)
    {
        var before = Sites(original, registry).ToArray();
        var after = Sites(finished, registry).ToArray();
        return before.Length == registry.Slots.Count && before.SequenceEqual(after);
    }

    static IEnumerable<string> Sites(string skeleton, InlineCodeTokens registry)
    {
        var tokens = SyntaxFactory.ParseTokens(skeleton).ToArray();
        var comments = tokens.SelectMany(token => token.LeadingTrivia.Concat(token.TrailingTrivia))
            .Where(trivia => registry.Slots.ContainsKey(trivia.ToString())).OrderBy(trivia => trivia.SpanStart);
        foreach (var comment in comments)
        {
            // Brace token positions exclude braces in generated literals and comments.
            var path = new Stack<int>();
            var opened = 0;
            foreach (var token in tokens.TakeWhile(token => token.SpanStart < comment.SpanStart))
            {
                if (token.RawKind == (int)SyntaxKind.OpenBraceToken) path.Push(++opened);
                if (token.RawKind == (int)SyntaxKind.CloseBraceToken && path.Count > 0) path.Pop();
            }

            yield return comment + ":" + string.Join(',', path.Reverse());
        }
    }

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
