// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Scriban.Parsing;
using Scriban.Syntax;

namespace Cratis.Stage.Rendering.Templates;

internal static class TemplateNesting
{
    internal static void Check(ScriptNode? root)
    {
        if (root is null) return;
        var pending = new Stack<(ScriptNode Node, int Blocks, int Expressions)>();
        pending.Push((root, 0, 0));
        while (pending.TryPop(out var frame))
        {
            var blocks = frame.Blocks + BlockDepth(frame.Node);
            var expressions = frame.Expressions + ExpressionDepth(frame.Node);
            if (Math.Max(blocks, expressions) > TemplateProfile.MaxDepth)
            {
                throw TemplateRefusal.At(TemplateCodes.Nesting, "Template nesting exceeds 32.", frame.Node.Span);
            }

            foreach (var child in Children(frame.Node)) pending.Push((child, blocks, expressions + EdgeDepth(frame.Node, child)));
        }
    }

    internal static bool IsParserLimit(string message) =>
        message.StartsWith("The statement depth limit `", StringComparison.Ordinal) ||
        message.Equals("The parser recursive depth limit was reached near a stack overflow", StringComparison.Ordinal);

    internal static TemplateRefusal ParserFailure(LogMessage error) =>
        TemplateRefusal.At(IsParserLimit(error.Message) ? TemplateCodes.Nesting : TemplateCodes.Parse, error.Message, error.Span);

    static IEnumerable<ScriptNode> Children(ScriptNode node)
    {
        for (var index = 0; index < node.ChildrenCount; index++)
        {
            var child = node.GetChildren(index);
            if (child is not null) yield return child;
        }
    }

    // Semantic block depth counts if/for/while/tablerow/capture/with/wrap/case and functions.
    // Anonymous do has one ScriptFunction child, so counts once. Else/else-if chains stay at their if's
    // level; ScriptBlockStatement wrappers and parentheses do not add block depth. All children,
    // including function bodies and expressions in parentheses, are traversed with an explicit stack.
    static int BlockDepth(ScriptNode node) => node switch
    {
        ScriptIfStatement { IsElseIf: true } => 0,
        ScriptIfStatement or ScriptForStatement or ScriptWhileStatement or ScriptCaptureStatement or
        ScriptWithStatement or ScriptWrapStatement or ScriptCaseStatement or ScriptFunction => 1,
        _ => 0
    };

    // Preserve the separate 32-level unary/delimiter expression bound without lexical keyword guesses.
    static int ExpressionDepth(ScriptNode node) => node is ScriptUnaryExpression or ScriptNestedExpression or
        ScriptArrayInitializerExpression or ScriptObjectInitializerExpression ? 1 : 0;

    // Indexers and calls are delimiters around their index or arguments, not around their target,
    // so chained member access and indexing (model.a[0][1]) stay shallow.
    static int EdgeDepth(ScriptNode parent, ScriptNode child) => parent switch
    {
        ScriptIndexerExpression indexer when ReferenceEquals(child, indexer.Index) => 1,
        ScriptFunctionCall call when ReferenceEquals(child, call.Arguments) => 1,
        _ => 0
    };
}
