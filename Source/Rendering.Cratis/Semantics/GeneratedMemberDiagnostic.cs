// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Identifies generated member casing mistakes without rewriting the submitted body.
/// </summary>
internal static class GeneratedMemberDiagnostic
{
    internal static string Hint(Diagnostic diagnostic, SemanticModel model)
    {
        if (diagnostic.Id is not ("CS1061" or "CS0117") || diagnostic.Location.SourceTree != model.SyntaxTree) return string.Empty;
        var node = model.SyntaxTree.GetRoot().FindNode(diagnostic.Location.SourceSpan);
        var access = node.AncestorsAndSelf().FirstOrDefault(candidate => candidate is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax);
        var (receiver, name) = access switch
        {
            MemberAccessExpressionSyntax member => (member.Expression, member.Name),
            MemberBindingExpressionSyntax member => (member.Ancestors().OfType<ConditionalAccessExpressionSyntax>().FirstOrDefault()?.Expression, member.Name),
            _ => (null, null)
        };
        if (receiver is null || name is null) return string.Empty;
        var type = model.GetTypeInfo(receiver).Type ?? model.GetSymbolInfo(receiver).Symbol as ITypeSymbol;
        if (type?.DeclaringSyntaxReferences.IsEmpty != false) return string.Empty;
        var authored = name.Identifier.ValueText;
        var matches = type.GetMembers().OfType<IPropertySymbol>().Where(member =>
            member.DeclaredAccessibility == Accessibility.Public && member.Name != authored &&
            string.Equals(member.Name, authored, StringComparison.OrdinalIgnoreCase)).ToArray();

        return matches.Length == 1
            ? $" Use generated member '{matches[0].Name}' instead of authored name '{authored}'."
            : string.Empty;
    }
}
