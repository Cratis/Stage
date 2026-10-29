// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class SemanticReactionEventBinding
{
    // Include nested functions and captures: a write anywhere invalidates the binding proof.
    internal static bool IsUnmodified(BlockSyntax body) => !body.DescendantNodes().Any(IsMutation);

    static bool IsMutation(SyntaxNode node) => node switch
    {
        AssignmentExpressionSyntax assignment => ContainsBinding(assignment.Left),
        ArgumentSyntax argument when !argument.RefKindKeyword.IsKind(SyntaxKind.None) => ContainsBinding(argument.Expression),
        RefExpressionSyntax reference => ContainsBinding(reference.Expression),
        PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression) => ContainsBinding(prefix.Operand),
        PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression) => ContainsBinding(postfix.Operand),
        _ => false
    };

    static bool ContainsBinding(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "event",
        ParenthesizedExpressionSyntax parenthesized => ContainsBinding(parenthesized.Expression),
        TupleExpressionSyntax tuple => tuple.Arguments.Any(argument => ContainsBinding(argument.Expression)),
        _ => false
    };
}
