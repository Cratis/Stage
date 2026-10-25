// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Decodes generated capture identifiers using the same lexical identity as C# declarations.
/// </summary>
internal static class SemanticCaptureIdentifier
{
    internal static bool TryDecode(string spelling, out string identity)
    {
        var token = SyntaxFactory.ParseToken(spelling);
        identity = string.Concat(token.ValueText.EnumerateRunes().Where(rune => Rune.GetUnicodeCategory(rune) != UnicodeCategory.Format));
        return token.IsKind(SyntaxKind.IdentifierToken) && !token.ContainsDiagnostics &&
            token.FullSpan.Length == spelling.Length && !token.HasLeadingTrivia && !token.HasTrailingTrivia;
    }

    internal static string Decode(string spelling) => TryDecode(spelling, out var identity) ? identity : spelling;

    internal static string Qualified(string spelling) => string.Join('.', spelling.Split('.').Select(Decode));
}
