// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>What an analysed pure body was compiled against, needed to decide the Common type exemption.</summary>
/// <param name="Context">The application context.</param>
/// <param name="Compilation">The analysis compilation.</param>
/// <param name="Declarations">Every declared type with its owning id and namespace.</param>
/// <param name="BoundTypes">The named types the body binds to.</param>
internal sealed record CommonTypeScope(
    SemanticApplicationContext Context,
    CSharpCompilation Compilation,
    IEnumerable<(SemanticId? Id, string Namespace, string Name)> Declarations,
    IEnumerable<INamedTypeSymbol> BoundTypes);

/// <summary>
/// Only authored Common concepts/composites can opt out of the conservative name guard.
/// Referenced types (including primitive CLR names) and slice-local peers never opt out.
/// </summary>
internal static class CommonTypeExemption
{
    internal static bool IsUnambiguous(string name, CommonTypeScope scope)
    {
        if (PureTransitionAdmission.ShadowsAuditedName(name)) return false;
        if (!OnlyCommonDeclares(name, scope)) return false;

        var commonType = scope.Compilation.GetTypeByMetadataName($"{scope.Context.CommonNamespace}.{name}");
        if (!BindsOnlyTo(name, commonType, scope)) return false;

        var imports = scope.Compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>()).ToArray();

        // Alias/static import lookup is not modeled by this narrow exemption.
        if (imports.Any(import => import.Alias is not null || import.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))) return false;
        return !ImportedNamespaces(imports).Any(path => ImportsCompetingType(name, path, commonType!, scope.Compilation));
    }

    static bool OnlyCommonDeclares(string name, CommonTypeScope scope)
    {
        var context = scope.Context;
        var commonIds = context.Application.Concepts.Where(concept => Identifiers.ToPascalCase(concept.Name) == name).Select(concept => concept.Id)
            .Concat(context.Application.Types.Where(type => Identifiers.ToPascalCase(type.Name) == name).Select(type => type.Id)).ToHashSet();
        return commonIds.Count != 0 && !scope.Declarations.Any(declaration => declaration.Name == name &&
            (declaration.Namespace != context.CommonNamespace || declaration.Id is not { } id || !commonIds.Contains(id)));
    }

    static bool BindsOnlyTo(string name, INamedTypeSymbol? commonType, CommonTypeScope scope)
    {
        var references = scope.BoundTypes.Where(type => type.Name == name).ToArray();
        return commonType is not null && references.Length != 0 && references.All(type => SymbolEqualityComparer.Default.Equals(type, commonType));
    }

    static IEnumerable<string> ImportedNamespaces(UsingDirectiveSyntax[] imports) =>
        imports.Select(import => import.Name?.ToString().Replace("global::", string.Empty, StringComparison.Ordinal))
            .OfType<string>().Append("System").Append(string.Empty).Distinct(StringComparer.Ordinal);

    // The merged namespace includes referenced assemblies and signature stand-ins.
    // Ignore only the exact Common declaration being considered, not other source types.
    static bool ImportsCompetingType(string name, string path, INamedTypeSymbol commonType, CSharpCompilation compilation)
    {
        var imported = compilation.GlobalNamespace;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            imported = imported?.GetNamespaceMembers().FirstOrDefault(candidate => candidate.Name == segment);
        }

        return imported?.GetTypeMembers(name).Any(type => !SymbolEqualityComparer.Default.Equals(type, commonType)) == true;
    }
}
