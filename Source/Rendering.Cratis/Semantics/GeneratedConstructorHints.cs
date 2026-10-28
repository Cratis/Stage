// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Builds named-argument guidance for an unambiguous generated constructor.
/// </summary>
internal static class GeneratedConstructorHints
{
    internal static string NamedArguments(SyntaxNode node, SemanticModel model)
    {
        var argument = node.AncestorsAndSelf().OfType<ArgumentSyntax>().FirstOrDefault();
        if (argument is null || argument.NameColon is not null) return string.Empty;
        if (argument.Parent is not ArgumentListSyntax arguments) return string.Empty;
        if (arguments.Parent is not (ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax)) return string.Empty;
        var info = model.GetSymbolInfo(arguments.Parent);
        var candidates = info.Symbol is IMethodSymbol method ? (IMethodSymbol[])[method] : [.. info.CandidateSymbols.OfType<IMethodSymbol>()];
        candidates = [.. candidates.Where(candidate => IsGeneratedConstructor(candidate) && MatchesArguments(candidate, arguments))];
        if (candidates.Length != 1) return string.Empty;
        var constructor = candidates[0];
        var parameters = string.Join(", ", constructor.Parameters.Select(parameter => $"{parameter.Name}: ..."));

        return $" Use named arguments: new {constructor.ContainingType.Name}({parameters}); generated constructor parameters follow semantic order, not declaration order.";
    }

    static bool IsGeneratedConstructor(IMethodSymbol candidate) =>
        candidate.MethodKind == MethodKind.Constructor && !candidate.ContainingType.DeclaringSyntaxReferences.IsEmpty;

    static bool MatchesArguments(IMethodSymbol candidate, ArgumentListSyntax arguments) =>
        candidate.Parameters.Length == arguments.Arguments.Count &&
        arguments.Arguments.All(argument => MatchesName(candidate, argument));

    static bool MatchesName(IMethodSymbol candidate, ArgumentSyntax argument) =>
        argument.NameColon is not { } name ||
        candidate.Parameters.Any(parameter => parameter.Name == name.Name.Identifier.ValueText);
}
