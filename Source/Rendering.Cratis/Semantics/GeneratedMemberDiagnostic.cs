// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Identifies generated member casing and concept construction mistakes without rewriting the submitted body.
/// </summary>
internal static class GeneratedMemberDiagnostic
{
    internal static string Hint(Diagnostic diagnostic, SemanticModel model)
    {
        if (diagnostic.Location.SourceTree != model.SyntaxTree) return string.Empty;
        var node = model.SyntaxTree.GetRoot().FindNode(diagnostic.Location.SourceSpan);
        if (diagnostic.Id == "CS1503") return ConceptConstructionHint(node, model);
        if (diagnostic.Id is not ("CS1061" or "CS0117")) return string.Empty;
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

    static string ConceptConstructionHint(SyntaxNode node, SemanticModel model)
    {
        var argument = node.AncestorsAndSelf().OfType<ArgumentSyntax>().FirstOrDefault();
        if (argument?.Parent is not ArgumentListSyntax arguments || arguments.Parent is not { } call) return string.Empty;
        var info = model.GetSymbolInfo(call);
        IMethodSymbol[] candidates = info.Symbol is IMethodSymbol method ? [method] : info.CandidateSymbols.OfType<IMethodSymbol>().ToArray();
        // Failed record construction can also list the copy constructor. Only a matching
        // argument shape identifies the parameter whose generated concept needs construction.
        candidates = candidates.Where(candidate => candidate.Parameters.Length == arguments.Arguments.Count &&
            arguments.Arguments.All(authoredArgument => authoredArgument.NameColon is not { } authoredName ||
                candidate.Parameters.Any(candidateParameter => candidateParameter.Name == authoredName.Name.Identifier.ValueText))).ToArray();
        if (candidates.Length != 1) return string.Empty;
        var parameters = candidates[0].Parameters;
        var index = arguments.Arguments.IndexOf(argument);
        var parameter = argument.NameColon is { } name
            ? parameters.FirstOrDefault(candidate => candidate.Name == name.Name.Identifier.ValueText)
            : index >= 0 && index < parameters.Length ? parameters[index] : null;
        if (parameter?.Type is not INamedTypeSymbol concept || concept.DeclaringSyntaxReferences.IsEmpty ||
            concept.BaseType is not { TypeArguments.Length: 1 } basis ||
            !((basis.Name == "ConceptAs" && basis.ContainingNamespace.ToDisplayString() == "Cratis.Concepts") ||
                (basis.Name == "EventSourceId" && basis.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Events"))) return string.Empty;
        var source = model.GetTypeInfo(argument.Expression).Type;
        if (!SymbolEqualityComparer.Default.Equals(source, basis.TypeArguments[0])) return string.Empty;
        return $" Use new {concept.Name}(...) to construct the generated concept explicitly.";
    }
}
