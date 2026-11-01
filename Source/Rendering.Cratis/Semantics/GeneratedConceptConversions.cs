// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Resolves the exact modeled concept operators trusted by one analysis compilation.
/// </summary>
internal static class GeneratedConceptConversions
{
    // Source ownership or a matching short name alone never grants an operator entry.
    internal static IReadOnlyDictionary<INamedTypeSymbol, ITypeSymbol> Resolve(SemanticApplicationContext context, Compilation compilation)
    {
        var trusted = new Dictionary<INamedTypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        var types = new SemanticTypeSystem(context);
        foreach (var concept in context.Application.Concepts.Where(concept => concept.Values.IsEmpty))
        {
            var symbol = compilation.GetTypeByMetadataName($"{context.CommonNamespace}.{Identifiers.ToPascalCase(concept.Name)}");
            var primitive = ResolvePrimitive(types.Primitive(concept.Primitive), compilation);
            if (symbol is null || primitive is null) continue;
            if (PureTransitionAdmission.Generated(symbol, compilation)) trusted.Add(symbol, primitive);
        }
        return trusted;
    }

    internal static bool Matches(ISymbol symbol, IReadOnlyDictionary<INamedTypeSymbol, ITypeSymbol> trusted) =>
        symbol is IMethodSymbol
        {
            MethodKind: MethodKind.Conversion, IsStatic: true,
            DeclaredAccessibility: Accessibility.Public, Parameters.Length: 1
        } method &&
        string.Equals(method.Name, "op_Implicit", StringComparison.Ordinal) &&
        trusted.TryGetValue(method.ContainingType, out var primitive) &&
        method.Parameters[0].RefKind == RefKind.None &&
        SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, primitive) &&
        SymbolEqualityComparer.Default.Equals(method.ReturnType, method.ContainingType);

    static ITypeSymbol? ResolvePrimitive(string scalar, Compilation compilation) => scalar switch
    {
        "string" => compilation.GetSpecialType(SpecialType.System_String),
        "int" => compilation.GetSpecialType(SpecialType.System_Int32),
        "long" => compilation.GetSpecialType(SpecialType.System_Int64),
        "decimal" => compilation.GetSpecialType(SpecialType.System_Decimal),
        "bool" => compilation.GetSpecialType(SpecialType.System_Boolean),
        _ => compilation.GetTypeByMetadataName(scalar.Replace("global::", string.Empty, StringComparison.Ordinal))
    };
}
