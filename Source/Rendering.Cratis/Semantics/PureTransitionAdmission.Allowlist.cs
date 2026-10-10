// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The audited member allowlist consulted for every symbol a pure body binds to.
/// </summary>
internal static partial class PureTransitionAdmission
{
    static bool Allowed(ISymbol symbol, Compilation compilation, string rootNamespace, out string entry)
    {
        entry = string.Empty;
        if (symbol is IMethodSymbol { MethodKind: MethodKind.BuiltinOperator } builtin)
        {
            entry = "primitive";
            return AllowedBuiltin(builtin);
        }

        // Anonymous records can carry randomized string hashes; no synthesized method is trusted.
        var type = symbol.ContainingType;
        if (type is null || IsUnadmittable(symbol, type))
        {
            return false;
        }

        if (IsConceptValueMember(symbol, type, compilation))
        {
            entry = "generated";
            return true;
        }

        if (IsSyntheticSource(symbol, type, compilation, rootNamespace))
        {
            entry = "generated";
            return AllowedGenerated(symbol, type, rootNamespace);
        }

        if (SafeValueType(type))
        {
            return AllowedValueType(symbol, type, out entry);
        }

        return AllowedLibrary(symbol, type, out entry);
    }

    static bool AllowedLibrary(ISymbol symbol, INamedTypeSymbol type, out string entry) =>
        AllowedValueLibrary(symbol, type, out entry) ??
        AllowedTextLibrary(symbol, type, out entry) ??
        AllowedCollectionLibrary(symbol, type, out entry) ??
        AllowedSystemLibrary(symbol, type, out entry) ??
        false;

    static bool IsUnadmittable(ISymbol symbol, INamedTypeSymbol type) => symbol.Name == "GetHashCode" || type.IsAnonymousType;

    static bool AllowedBuiltin(IMethodSymbol builtin) =>
        builtin.Parameters.All(parameter => SafeValueType(parameter.Type)) && SafeValueType(builtin.ReturnType);

    // The pinned ConceptAs<T> and EventSourceId<T> expose auto-property getters only.
    // Mirror their declaring types in the synthetic compilation, rather than admitting a
    // synthetic owned Value that binds differently from the real inherited property.
    static bool IsConceptValueMember(ISymbol symbol, INamedTypeSymbol type, Compilation compilation)
    {
        var genericOwner = type.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var name = symbol.Name;
        return ((genericOwner == "global::Cratis.Concepts.ConceptAs<T>" && name == "Value") ||
                (genericOwner == "global::Cratis.Chronicle.Events.EventSourceId<T>" && name == "TypedValue")) &&
            symbol is IPropertySymbol { IsStatic: false, IsIndexer: false, GetMethod: not null } conceptProperty &&
            SymbolEqualityComparer.Default.Equals(conceptProperty.Type, type.TypeArguments[0]) &&
            SafeValue(conceptProperty.Type, compilation);
    }

    // Both the declaring type and the member must belong to this exact synthetic source tree.
    // This excludes Microsoft.Win32, all referenced application assemblies and unrelated source files.
    static bool IsSyntheticSource(ISymbol symbol, INamedTypeSymbol type, Compilation compilation, string rootNamespace)
    {
        var typeNamespace = type.ContainingNamespace.ToDisplayString();
        return type.ContainingAssembly.Name == compilation.AssemblyName &&
            type.Locations.Length > 0 && type.Locations.All(_ => _.IsInSource && compilation.SyntaxTrees.Contains(_.SourceTree)) &&
            (typeNamespace == rootNamespace || typeNamespace.StartsWith(rootNamespace + ".", StringComparison.Ordinal)) &&
            symbol.Locations.All(_ => _.IsInSource && compilation.SyntaxTrees.Contains(_.SourceTree));
    }

    static bool AllowedGenerated(ISymbol symbol, INamedTypeSymbol type, string rootNamespace)
    {
        var full = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var name = symbol.Name;
        if (IsTenantMember(symbol, full, rootNamespace) || IsIdentityLookup(symbol, full, rootNamespace))
        {
            return true;
        }

        return name != "ToString" &&
            (type.Name.StartsWith("TypedContext_", StringComparison.Ordinal)
                ? symbol is IPropertySymbol
                : symbol is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.PropertyGet } or
                    IPropertySymbol or IFieldSymbol { IsConst: true });
    }

    static bool IsTenantMember(ISymbol symbol, string full, string rootNamespace) =>
        full == $"global::{rootNamespace}.TypedContexts.TenantId" &&
        ((symbol is IFieldSymbol { IsStatic: true, IsReadOnly: true } && NameIs(symbol.Name, "Default", "NotSet")) ||
            (symbol is IMethodSymbol { IsImplicitlyDeclared: true, MethodKind: MethodKind.UserDefinedOperator } && NameIs(symbol.Name, "op_Equality", "op_Inequality")));

    // Stage's policy Identity mirrors Screenplay's audited lookups: ordinal roles, ordinal-ignore-case claim names.
    static bool IsIdentityLookup(ISymbol symbol, string full, string rootNamespace) =>
        full == $"global::{rootNamespace}.TypedContexts.{SemanticPolicyContextRuntime.IdentityType}" &&
        symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary, IsStatic: false, Parameters.Length: 1 } lookup &&
        lookup.Parameters[0].Type.SpecialType == SpecialType.System_String &&
        NameIs(symbol.Name, "HasRole", "HasClaim", "ClaimValue", "ClaimValues");

    static bool AllowedValueType(ISymbol symbol, INamedTypeSymbol type, out string entry)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            entry = "string";
            return AllowedStringMember(symbol);
        }

        entry = "primitive";
        return symbol is IMethodSymbol { Parameters.Length: 1 } method &&
            method.Name == "ToString" &&
            method.Parameters[0].Type.Name == "IFormatProvider" &&
            method.Parameters[0].Type.ContainingNamespace.ToDisplayString() == "System";
    }

    static bool AllowedStringMember(ISymbol symbol) => symbol switch
    {
        IPropertySymbol textProperty => symbol.Name == "Length" || textProperty.IsIndexer,
        IFieldSymbol => symbol.Name == "Empty",
        IMethodSymbol method => AllowedStringMethod(method),
        _ => false
    };

    static bool AllowedStringMethod(IMethodSymbol method)
    {
        var name = method.Name;
        return (name == "Substring" && method.Parameters.Length is 1 or 2 &&
                method.Parameters.All(parameter => parameter.Type.SpecialType == SpecialType.System_Int32)) ||
            NameIs(name, "IsNullOrEmpty", "op_Equality", "op_Inequality") ||
            (name == "Concat" && method.Parameters.All(parameter => parameter.Type.SpecialType == SpecialType.System_String)) ||
            (NameIs(name, "Equals", "StartsWith", "EndsWith", "Contains", "IndexOf") &&
                method.Parameters.Any(parameter => parameter.Type.ToDisplayString() == "System.StringComparison"));
    }

    // Returns null when the type is not one of these audited value libraries.
    static bool? AllowedValueLibrary(ISymbol symbol, INamedTypeSymbol type, out string entry)
    {
        var full = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var name = symbol.Name;
        entry = string.Empty;
        if (full.StartsWith("global::System.Nullable<", StringComparison.Ordinal))
        {
            entry = "primitive";
            return SafeValueType(type.TypeArguments[0]) && NameIs(name, "HasValue", "Value", "GetValueOrDefault", ".ctor");
        }

        if (full == "global::System.Math")
        {
            entry = "math";
            return AllowedMath(symbol);
        }

        if (NameIs(full, "global::System.DateTimeOffset", "global::System.TimeSpan"))
        {
            entry = "time";
            return AllowedTime(symbol);
        }

        return null;
    }

    // Returns null when the type is not one of these audited text-related libraries.
    static bool? AllowedTextLibrary(ISymbol symbol, INamedTypeSymbol type, out string entry)
    {
        var full = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var name = symbol.Name;
        entry = "string";
        if (full == "global::System.StringComparison")
        {
            return symbol is IFieldSymbol && name == "Ordinal";
        }

        if (full == "global::System.Globalization.CultureInfo")
        {
            return symbol is IPropertySymbol && name == "InvariantCulture";
        }

        entry = string.Empty;
        return null;
    }

    // Returns null when the type is not one of these audited collection, LINQ or exception libraries.
    static bool? AllowedCollectionLibrary(ISymbol symbol, INamedTypeSymbol type, out string entry)
    {
        var full = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var name = symbol.Name;
        entry = string.Empty;
        if (full == "global::System.Collections.Immutable.ImmutableArray")
        {
            entry = "collections";
            return name == "ToImmutableArray" && symbol is IMethodSymbol { Parameters.Length: 1 } method &&
                method.Parameters[0].Type.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>";
        }

        if (full.StartsWith("global::System.Collections.", StringComparison.Ordinal))
        {
            entry = "collections";
            return AllowedCollectionMember(symbol, full);
        }

        if (full == "global::System.Linq.Enumerable")
        {
            entry = "linq";
            return AllowedLinq(symbol);
        }

        return null;
    }

    // Returns null when the type is not one of these audited exception or rounding types.
    static bool? AllowedSystemLibrary(ISymbol symbol, INamedTypeSymbol type, out string entry)
    {
        var full = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        entry = string.Empty;
        if (NameIs(full, "global::System.InvalidOperationException", "global::System.ArgumentException"))
        {
            entry = "exceptions";
            return symbol is IMethodSymbol { MethodKind: MethodKind.Constructor, Parameters.Length: 1 } ctor &&
                ctor.Parameters[0].Type.SpecialType == SpecialType.System_String;
        }

        if (full == "global::System.MidpointRounding")
        {
            entry = "math";
            return symbol is IFieldSymbol { IsConst: true } && NameIs(symbol.Name, "ToEven", "AwayFromZero", "ToZero", "ToNegativeInfinity", "ToPositiveInfinity");
        }

        return null;
    }

    static bool AllowedMath(ISymbol symbol) => IsNumericMath(symbol) || IsDecimalRound(symbol) || IsDecimalRounding(symbol);

    static bool IsNumericMath(ISymbol symbol) =>
        symbol is IMethodSymbol math && math.Parameters.Length > 0 &&
        math.Parameters.All(parameter => Number(parameter.Type)) && Number(math.ReturnType) &&
        NameIs(symbol.Name, "Abs", "Min", "Max", "Clamp", "Sign");

    static bool IsDecimalRound(ISymbol symbol) =>
        symbol is IMethodSymbol { Parameters.Length: 2 } rounded && symbol.Name == "Round" &&
        rounded.Parameters[0].Type.SpecialType == SpecialType.System_Decimal &&
        rounded.Parameters[1].Type.ToDisplayString() == "System.MidpointRounding" &&
        rounded.ReturnType.SpecialType == SpecialType.System_Decimal;

    static bool IsDecimalRounding(ISymbol symbol) =>
        symbol is IMethodSymbol { Parameters.Length: 1 } decimalMath &&
        NameIs(symbol.Name, "Truncate", "Floor", "Ceiling") &&
        decimalMath.Parameters[0].Type.SpecialType == SpecialType.System_Decimal &&
        decimalMath.ReturnType.SpecialType == SpecialType.System_Decimal;

    static bool AllowedTime(ISymbol symbol)
    {
        var name = symbol.Name;
        if (symbol is IPropertySymbol) return NameIs(name, "Year", "Month", "Day", "Hour", "Minute", "Second", "Ticks");
        return symbol is IMethodSymbol method &&
            ((NameIs(name, "op_Equality", "op_Inequality", "op_LessThan", "op_LessThanOrEqual", "op_GreaterThan", "op_GreaterThanOrEqual", "op_Addition", "op_Subtraction") &&
                method.Parameters.All(parameter => NameIs(parameter.Type.ToDisplayString(), "System.DateTimeOffset", "System.TimeSpan"))) ||
             (NameIs(name, "Add", "AddDays", "AddHours", "AddMinutes", "AddSeconds", "AddMilliseconds", "AddTicks", "AddMonths", "AddYears") &&
                method.Parameters.Length == 1 &&
                (Number(method.Parameters[0].Type) || method.Parameters[0].Type.ToDisplayString() == "System.TimeSpan")));
    }

    static bool AllowedCollectionMember(ISymbol symbol, string full)
    {
        var name = symbol.Name;
        var immutableArray = full.StartsWith("global::System.Collections.Immutable.ImmutableArray<", StringComparison.Ordinal);
        return (immutableArray && (NameIs(name, "Length", "IsDefault", "Empty", "GetEnumerator") || symbol is IPropertySymbol { IsIndexer: true })) ||
            (full.StartsWith("global::System.Collections.Generic.IEnumerable<", StringComparison.Ordinal) && name == "GetEnumerator") ||
            (immutableArray && NameIs(name, "Current", "MoveNext"));
    }

    static bool AllowedLinq(ISymbol symbol) =>
        NameIs(symbol.Name, "Where", "Select", "Any", "All", "Count", "First", "FirstOrDefault", "Last", "LastOrDefault", "Take", "Skip", "Sum", "Aggregate", "ToArray", "Concat") &&
        (symbol.Name != "Aggregate" || symbol is IMethodSymbol { Parameters.Length: 3 }) &&
        (symbol.Name != "Sum" || (symbol is IMethodSymbol sum && DecimalSumReturn(sum.ReturnType)));
}
