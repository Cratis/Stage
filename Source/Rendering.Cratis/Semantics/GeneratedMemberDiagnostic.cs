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
        var candidates = info.Symbol is IMethodSymbol method ? (IMethodSymbol[])[method] : [.. info.CandidateSymbols.OfType<IMethodSymbol>()];

        // Failed record construction can also list the copy constructor. Only a matching
        // argument shape identifies the parameter whose generated concept needs construction.
        candidates = [.. candidates.Where(candidate => candidate.Parameters.Length == arguments.Arguments.Count &&
            arguments.Arguments.All(authoredArgument => authoredArgument.NameColon is not { } authoredName ||
                candidate.Parameters.Any(candidateParameter => candidateParameter.Name == authoredName.Name.Identifier.ValueText)))];
        if (candidates.Length != 1) return string.Empty;
        var parameters = candidates[0].Parameters;
        var index = arguments.Arguments.IndexOf(argument);
        var parameter = argument.NameColon switch
        {
            { } name => parameters.FirstOrDefault(candidate => candidate.Name == name.Name.Identifier.ValueText),
            _ when index >= 0 && index < parameters.Length => parameters[index],
            _ => null
        };
        var concept = parameter?.Type as INamedTypeSymbol;
        var underlying = ConceptUnderlyingType(concept);
        if (underlying is null && candidates[0].MethodKind == MethodKind.Constructor)
        {
            concept = candidates[0].ContainingType;
            underlying = ConceptUnderlyingType(concept);
            if (!SymbolEqualityComparer.Default.Equals(parameter?.Type, underlying)) return string.Empty;
        }
        if (concept is null || underlying is null) return string.Empty;

        var source = model.GetTypeInfo(argument.Expression).Type;
        if (source?.SpecialType == SpecialType.System_String && underlying.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Guid")
        {
            return $" Use new {concept.Name}(Guid.Parse(...)) to construct the generated concept explicitly.";
        }
        if (!SymbolEqualityComparer.Default.Equals(source, underlying)) return string.Empty;

        return $" Use new {concept.Name}(...) to construct the generated concept explicitly.";
    }

    static ITypeSymbol? ConceptUnderlyingType(INamedTypeSymbol? concept)
    {
        if (concept?.DeclaringSyntaxReferences.IsEmpty != false) return null;
        if (concept.BaseType is not { TypeArguments.Length: 1 } basis) return null;

        return (basis.Name, basis.ContainingNamespace.ToDisplayString()) switch
        {
            ("ConceptAs", "Cratis.Concepts") or ("EventSourceId", "Cratis.Chronicle.Events") => basis.TypeArguments[0],
            _ => null
        };
    }
}
