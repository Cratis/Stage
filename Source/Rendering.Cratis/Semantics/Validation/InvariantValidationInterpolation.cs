// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Validation;

/// <summary>
/// Makes each admitted iterator interpolation invariant, including nested and locally assigned strings.
/// The purity walk checks the original hole expressions and permits only audited scalar formatting types.
/// </summary>
internal sealed class InvariantValidationInterpolation : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
    {
        if (node.Contents.All(content => content is InterpolatedStringTextSyntax)) return node;
        var rewritten = (InterpolatedStringExpressionSyntax)base.VisitInterpolatedStringExpression(node)!;

        // A global:: alias in an interpolation hole needs parentheses: otherwise ':' starts its format clause.
        var call = SyntaxFactory.InvocationExpression(
            SyntaxFactory.ParseExpression("global::System.FormattableString.Invariant"),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(rewritten.WithoutTrivia()))));
        return SyntaxFactory.ParenthesizedExpression(call).WithTriviaFrom(node);
    }

    internal static string Render(string body)
    {
        var block = SyntaxFactory.ParseStatement("{\n" + body + "\n}", consumeFullText: true);
        return new InvariantValidationInterpolation().Visit(block).ToFullString()[2..^2];
    }
}
