// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

/// <summary>
/// Compares what every name, invocation, object creation, attribute, await and foreach of a file binds to before and
/// after a rewrite. The rewrite only removes qualifiers and replaces the using directives, so every node outside an
/// edit has a counterpart of the same kind at the mapped position.
/// </summary>
static class BindingSites
{
    static readonly SymbolDisplayFormat _format = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    /// <summary>
    /// Compares the bindings of a file.
    /// </summary>
    /// <param name="original">The semantic model of today's file.</param>
    /// <param name="rewritten">The semantic model of the rewritten file.</param>
    /// <param name="changes">The changes that turned one into the other.</param>
    /// <returns>A description of every binding that differs.</returns>
    public static IEnumerable<string> Differences(SemanticModel original, SemanticModel rewritten, ImmutableArray<TextChange> changes)
    {
        var counterparts = Sites(rewritten.SyntaxTree.GetRoot())
            .GroupBy(node => (node.RawKind, node.SpanStart))
            .ToDictionary(group => group.Key, group => group.OrderByDescending(node => node.Span.Length).ToArray());
        foreach (var group in Sites(original.SyntaxTree.GetRoot()).Where(node => !changes.Any(change => Removes(change.Span, node.Span)))
            .GroupBy(node => (node.RawKind, node.SpanStart)))
        {
            var nodes = group.OrderByDescending(node => node.Span.Length).ToArray();
            var mapped = counterparts.GetValueOrDefault((group.Key.RawKind, Map(changes, group.Key.SpanStart))) ?? [];
            for (var index = 0; index < nodes.Length; index++)
            {
                var before = Describe(original, nodes[index]);
                var after = index < mapped.Length ? Describe(rewritten, mapped[index]) : "<missing>";
                if (before != after) yield return $"{original.SyntaxTree.FilePath}: '{nodes[index]}' bound to {before} but rewritten to {after}";
            }
        }
    }

    static IEnumerable<SyntaxNode> Sites(SyntaxNode root) => root.DescendantNodes().Where(node => node is SimpleNameSyntax or InvocationExpressionSyntax
        or BaseObjectCreationExpressionSyntax or AttributeSyntax or AwaitExpressionSyntax or ForEachStatementSyntax);

    // A node inside a replaced span, such as a removed qualifier or an old using directive, has no counterpart.
    static bool Removes(TextSpan change, TextSpan node) =>
        change.Length > 0 && (change.Contains(node) || (node.Start > change.Start && node.Start < change.End));

    static int Map(ImmutableArray<TextChange> changes, int position) => position + changes
        .Where(change => change.Span.End <= position && !(change.Span.Start == position && change.Span.Length > 0))
        .Sum(change => change.NewText!.Length - change.Span.Length);

    static string Describe(SemanticModel model, SyntaxNode node) => node switch
    {
        AwaitExpressionSyntax await => Describe(model.GetAwaitExpressionInfo(await).GetAwaiterMethod),
        ForEachStatementSyntax loop => Describe(model.GetForEachStatementInfo(loop).GetEnumeratorMethod),
        _ => Describe(model.GetSymbolInfo(node))
    };

    static string Describe(SymbolInfo info) => info.Symbol is { } symbol
        ? Describe(symbol)
        : $"none({info.CandidateReason}: {string.Join(", ", info.CandidateSymbols.Select(Describe))})";

    static string Describe(ISymbol? symbol) => symbol switch
    {
        null => "none",
        IMethodSymbol { ReducedFrom: { } extension } => $"extension {extension.ToDisplayString(_format)}",
        _ => $"{symbol.Kind} {symbol.ToDisplayString(_format)}"
    };
}
#endif
