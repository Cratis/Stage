// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Identifies generated member casing and constructor argument mistakes without rewriting the submitted body.
/// </summary>
internal static class GeneratedMemberDiagnostic
{
    internal static string Hint(Diagnostic diagnostic, SemanticModel model)
    {
        if (diagnostic.Location.SourceTree != model.SyntaxTree) return string.Empty;
        var node = model.SyntaxTree.GetRoot().FindNode(diagnostic.Location.SourceSpan);
        return diagnostic.Id switch
        {
            "CS1503" => ConceptValueHint(node, model) + ConceptConstructionHint(node, model) + GeneratedConstructorHints.NamedArguments(node, model),
            "CS0029" or "CS0266" => ConceptValueHint(node, model),
            "CS1061" or "CS0117" => MemberCasingHint(node, model),
            _ => string.Empty
        };
    }

    static string MemberCasingHint(SyntaxNode node, SemanticModel model)
    {
        var (receiver, name) = MemberAccess(node);
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

    static (ExpressionSyntax? Receiver, SimpleNameSyntax? Name) MemberAccess(SyntaxNode node) =>
        node.AncestorsAndSelf().FirstOrDefault(candidate => candidate is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax) switch
        {
            MemberAccessExpressionSyntax member => (member.Expression, member.Name),
            MemberBindingExpressionSyntax member => (member.Ancestors().OfType<ConditionalAccessExpressionSyntax>().FirstOrDefault()?.Expression, member.Name),
            _ => (null, null)
        };

    static string ConceptValueHint(SyntaxNode node, SemanticModel model)
    {
        var expression = node.AncestorsAndSelf().OfType<ExpressionSyntax>().FirstOrDefault();
        if (expression is null) return string.Empty;
        var info = model.GetTypeInfo(expression);
        return info.Type is INamedTypeSymbol concept && ConceptUnderlyingType(concept) is { } primitive &&
            SymbolEqualityComparer.Default.Equals(info.ConvertedType, primitive)
                ? $" Use .Value to read the underlying value of generated concept '{concept.Name}'."
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
