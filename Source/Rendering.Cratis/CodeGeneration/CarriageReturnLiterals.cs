// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Finds carriage returns that sit inside string or character literal tokens, where
/// the published LF normalization would change the literal's value.
/// </summary>
internal static class CarriageReturnLiterals
{
    /// <summary>
    /// Finds the first literal token containing a carriage return.
    /// </summary>
    /// <param name="body">The authored C# statement-block contents.</param>
    /// <returns>A description of the literal, or null when every carriage return is outside literals.</returns>
    internal static string? Find(string body)
    {
        if (!body.Contains('\r')) return null;
        var tree = CSharpSyntaxTree.ParseText("class C { void M() {\n" + body + "\n} }");
        foreach (var token in tree.GetRoot().DescendantTokens())
        {
            if (IsLiteralText(token.Kind()) && token.Text.Contains('\r'))
            {
                var line = tree.GetLineSpan(token.Span).StartLinePosition.Line;
                var preview = token.Text.Split('\r', '\n')[0];
                return $"{Describe(token.Kind())} starting at body line {line} ('{(preview.Length > 40 ? preview[..40] : preview)}')";
            }
        }

        return null;
    }

    static bool IsLiteralText(SyntaxKind kind) => kind is
        SyntaxKind.StringLiteralToken or SyntaxKind.CharacterLiteralToken or
        SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken or
        SyntaxKind.Utf8StringLiteralToken or SyntaxKind.Utf8SingleLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken or
        SyntaxKind.InterpolatedStringTextToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken or SyntaxKind.InterpolatedRawStringEndToken;

    static string Describe(SyntaxKind kind) => kind switch
    {
        SyntaxKind.CharacterLiteralToken => "character literal",
        SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken
            or SyntaxKind.Utf8SingleLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken => "raw string literal",
        SyntaxKind.InterpolatedStringTextToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken or SyntaxKind.InterpolatedRawStringEndToken => "interpolated string literal",
        _ => "string literal"
    };
}
