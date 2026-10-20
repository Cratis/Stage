// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Infers an inline handler's signature from its own return and yield statements.
/// </summary>
internal static class InlineHandlerReturnType
{
    const string Enumerable = "IEnumerable<object>";

    /// <summary>
    /// Infers the return type without binding authored types or inspecting nested functions.
    /// </summary>
    /// <param name="code">The authored C# body.</param>
    /// <returns>The explicit constructed type, void, or the legacy event collection type.</returns>
    public static string Infer(string code)
    {
        var text = "{\n" + code + "\n}";
        var statement = SyntaxFactory.ParseStatement(text, options: new CSharpParseOptions(), consumeFullText: true);
        if (statement is not BlockSyntax body || body.ContainsDiagnostics || body.ContainsSkippedText ||
            body.CloseBraceToken.IsMissing || body.CloseBraceToken.SpanStart != text.Length - 1)
        {
            return Enumerable;
        }

        var statements = body.DescendantNodes(node => node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax).ToArray();
        if (statements.OfType<YieldStatementSyntax>().Any())
        {
            return Enumerable;
        }

        var returns = statements.OfType<ReturnStatementSyntax>().Where(@return => @return.Expression is not null).ToArray();
        if (returns.Length == 0)
        {
            return "void";
        }

        var types = returns.Select(@return => (@return.Expression as ObjectCreationExpressionSyntax)?.Type.NormalizeWhitespace().ToFullString().Trim()).Distinct(StringComparer.Ordinal).ToArray();
        return types.Length == 1 && types[0] is { } type ? type : Enumerable;
    }
}
