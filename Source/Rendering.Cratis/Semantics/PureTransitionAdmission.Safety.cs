// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Operand and conversion safety checks for the operators in a pure body.
/// </summary>
internal static partial class PureTransitionAdmission
{
    static bool SafeBinary(IBinaryOperation operation, Compilation compilation)
    {
        if (operation.OperatorKind == BinaryOperatorKind.Add && IsTextOperands(operation))
        {
            return true;
        }

        if (IsUnsafeOperand(operation.LeftOperand, compilation) || IsUnsafeOperand(operation.RightOperand, compilation))
        {
            return false;
        }

        return AreComparableOperands(operation);
    }

    static bool IsTextOperands(IBinaryOperation operation) =>
        operation.Type?.SpecialType == SpecialType.System_String &&
        StringOrChar(operation.LeftOperand) && StringOrChar(operation.RightOperand);

    static bool IsUnsafeOperand(IOperation operand, Compilation compilation) =>
        !SafeValue(operand.Type, compilation) && operand.ConstantValue is not { HasValue: true, Value: null };

    static bool AreComparableOperands(IBinaryOperation operation)
    {
        var left = operation.LeftOperand.Type;
        var right = operation.RightOperand.Type;
        return (Number(left) && Number(right)) ||
            (left?.SpecialType == SpecialType.System_Boolean && right?.SpecialType == SpecialType.System_Boolean) ||
            IsTextOperands(operation) ||
            (IsTimeType(left) && IsTimeType(right)) ||
            IsEqualityOnSameType(operation);
    }

    static bool IsEqualityOnSameType(IBinaryOperation operation)
    {
        var left = operation.LeftOperand.Type;
        var right = operation.RightOperand.Type;
        return operation.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals &&
            (SymbolEqualityComparer.Default.Equals(left, right) || left is null || right is null);
    }

    static bool IsTimeType(ITypeSymbol? type) =>
        NameIs(type?.ToDisplayString() ?? string.Empty, "System.DateTimeOffset", "System.TimeSpan");

    static bool SafeConversion(IConversionOperation operation)
    {
        if (operation.OperatorMethod is not null) return false;
        if (operation.Operand.ConstantValue.HasValue && operation.Operand.ConstantValue.Value is null) return true;
        var from = operation.Operand.Type;
        var to = operation.Type;
        if (from is null || to is null) return false;
        if (IsAlwaysSafeConversion(operation, from, to)) return true;
        if (Number(from) && Number(to)) return IsSafeNumericConversion(operation, from, to);
        if (IsNullableUnwrap(from, to) || IsNullableWrap(from, to)) return operation.Conversion.IsImplicit;
        return IsEnumerableConversion(operation, from, to);
    }

    static bool IsSafeNumericConversion(IConversionOperation operation, ITypeSymbol from, ITypeSymbol to) =>
        operation.Conversion.IsImplicit || from.SpecialType == SpecialType.System_Decimal || to.SpecialType == SpecialType.System_Decimal;

    static bool IsEnumerableConversion(IConversionOperation operation, ITypeSymbol from, ITypeSymbol to) =>
        operation.Conversion.IsImplicit && to is INamedTypeSymbol target &&
        target.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>" &&
        (from is IArrayTypeSymbol || from.ToDisplayString().StartsWith("System.Collections.Immutable.ImmutableArray<", StringComparison.Ordinal));

    static bool IsAlwaysSafeConversion(IConversionOperation operation, ITypeSymbol from, ITypeSymbol to) =>
        (operation.Parent is IThrowOperation && operation.Conversion.IsImplicit && ValidThrow(operation.Operand)) ||
        (operation.Parent is IForEachLoopOperation && from is IArrayTypeSymbol && to.ToDisplayString() == "System.Collections.IEnumerable") ||
        (to.Name == "IFormatProvider" && IsInvariantProvider(operation.Operand) && operation.Conversion.IsImplicit) ||
        SymbolEqualityComparer.Default.Equals(from, to) ||
        IsImplicitCharToNumber(operation, from, to) ||
        IsCharInTextConcatenation(operation, from, to);

    static bool IsImplicitCharToNumber(IConversionOperation operation, ITypeSymbol from, ITypeSymbol to) =>
        from.SpecialType == SpecialType.System_Char && Number(to) && operation.Conversion.IsImplicit;

    static bool IsCharInTextConcatenation(IConversionOperation operation, ITypeSymbol from, ITypeSymbol to) =>
        from.SpecialType == SpecialType.System_Char && to.SpecialType == SpecialType.System_Object &&
        operation.Parent is ICompoundAssignmentOperation { Type.SpecialType: SpecialType.System_String } or
            IBinaryOperation { Type.SpecialType: SpecialType.System_String, OperatorKind: BinaryOperatorKind.Add };

    static bool IsNullableUnwrap(ITypeSymbol from, ITypeSymbol to) =>
        from is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable &&
        SymbolEqualityComparer.Default.Equals(nullable.TypeArguments[0], to);

    static bool IsNullableWrap(ITypeSymbol from, ITypeSymbol to) =>
        to is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable &&
        SymbolEqualityComparer.Default.Equals(from, nullable.TypeArguments[0]);
}
