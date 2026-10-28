// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Combines file declarations and checks the plan-wide constraints that cannot be decided one file at a time.
/// </summary>
static class JavaPlanRegistry
{
    internal static ImmutableDictionary<TemplateSymbol, JavaDeclaration> Build(ImmutableArray<DeclarativeLanguageFile> files)
    {
        var declarations = ImmutableDictionary.CreateBuilder<TemplateSymbol, JavaDeclaration>();
        foreach (var file in files)
        {
            foreach (var symbol in JavaFileStructure.Declarations(file))
            {
                if (!declarations.TryAdd(symbol, new(symbol, symbol, file.Path)))
                {
                    throw new LanguagePackRefusal(JavaRefusalCodes.DuplicateDeclaration, $"Java symbol '{symbol.Namespace}.{symbol.Name}' is declared in more than one file.");
                }
            }
        }

        var result = declarations.ToImmutable();
        ValidatePackages(result.Keys);
        ValidateTopLevelNames(result.Keys);
        return result;
    }

    static void ValidatePackages(IEnumerable<TemplateSymbol> symbols)
    {
        var packages = symbols.SelectMany(symbol => PackagePrefixes(symbol.Namespace)).Distinct(StringComparer.Ordinal).ToArray();
        if (packages.GroupBy(package => package, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.CaseCollision, "Plan packages differ only in ordinal case.");
        }

        var packageSet = packages.ToImmutableHashSet(StringComparer.Ordinal);
        foreach (var top in symbols.Where(symbol => !symbol.Name.Contains('.', StringComparison.Ordinal)))
        {
            if (packageSet.Contains(top.Namespace + "." + top.Name))
            {
                throw new LanguagePackRefusal(JavaRefusalCodes.PackageTypeClash, $"Java type '{top.Namespace}.{top.Name}' is also a plan package.");
            }
        }
    }

    static IEnumerable<string> PackagePrefixes(string package)
    {
        for (var index = 0; index < package.Length; index++)
        {
            if (package[index] == '.')
            {
                yield return package[..index];
            }
        }

        yield return package;
    }

    static void ValidateTopLevelNames(IEnumerable<TemplateSymbol> symbols)
    {
        foreach (var package in symbols.Where(symbol => !symbol.Name.Contains('.', StringComparison.Ordinal)).GroupBy(symbol => symbol.Namespace, StringComparer.Ordinal))
        {
            if (package.GroupBy(symbol => symbol.Name, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            {
                throw new LanguagePackRefusal(JavaRefusalCodes.CaseCollision, $"Top-level Java type names in '{package.Key}' differ only in ordinal case.");
            }
        }
    }
}
