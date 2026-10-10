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

    static bool AreComparableOperands(IBinaryOperation operation) =>
        AreNumericOperands(operation) ||
        AreBooleanOperands(operation) ||
        IsTextOperands(operation) ||
        AreTimeOperands(operation) ||
        IsEqualityOnSameType(operation);

    static bool AreNumericOperands(IBinaryOperation operation) =>
        Number(operation.LeftOperand.Type) && Number(operation.RightOperand.Type);

    static bool AreBooleanOperands(IBinaryOperation operation) =>
        operation.LeftOperand.Type?.SpecialType == SpecialType.System_Boolean &&
        operation.RightOperand.Type?.SpecialType == SpecialType.System_Boolean;

    static bool AreTimeOperands(IBinaryOperation operation) =>
        IsTimeType(operation.LeftOperand.Type) && IsTimeType(operation.RightOperand.Type);

    static bool IsEqualityOnSameType(IBinaryOperation operation)
    {
        var left = operation.LeftOperand.Type;
        var right = operation.RightOperand.Type;
        return operation.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals &&
            (SymbolEqualityComparer.Default.Equals(left, right) || left is null || right is null);
    }

    static bool IsTimeType(ITypeSymbol? type) =>
        NameIs(type?.ToDisplayString() ?? string.Empty, "System.DateTimeOffset", "System.TimeSpan");

    static bool SafeConversion(IConversionOperation operation) =>
        operation.OperatorMethod is null && (IsNullConstant(operation) || IsSafeTypedConversion(operation));

    static bool IsNullConstant(IConversionOperation operation) =>
        operation.Operand.ConstantValue.HasValue && operation.Operand.ConstantValue.Value is null;

    static bool IsSafeTypedConversion(IConversionOperation operation) =>
        operation.Operand.Type is { } from && operation.Type is { } to &&
        (IsAlwaysSafeConversion(operation, from, to) ||
            (Number(from) && Number(to)
                ? IsSafeNumericConversion(operation, from, to)
                : IsSafeReferenceConversion(operation, from, to)));

    static bool IsSafeReferenceConversion(IConversionOperation operation, ITypeSymbol from, ITypeSymbol to) =>
        IsNullableUnwrap(from, to) || IsNullableWrap(from, to)
            ? operation.Conversion.IsImplicit
            : IsEnumerableConversion(operation, from, to);

    static bool IsSafeNumericConversion(IConversionOperation operation, ITypeSymbol from, ITypeSymbol to) =>
        operation.Conversion.IsImplicit || from.SpecialType == SpecialType.System_Decimal || to.SpecialType == SpecialType.System_Decimal;

    static bool IsEnumerableConversion(IConversionOperation operation, ITypeSymbol from, ITypeSymbol to) =>
        operation.Conversion.IsImplicit && to is INamedTypeSymbol target &&
        target.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>" &&
        (from is IArrayTypeSymbol || from.ToDisplayString().StartsWith("System.Collections.Immutable.ImmutableArray<", StringComparison.Ordinal));

    static bool IsAlwaysSafeConversion(IConversionOperation operation, ITypeSymbol from, ITypeSymbol to) =>
        IsThrowOrForeachConversion(operation, from, to) ||
        IsInvariantProviderConversion(operation, to) ||
        SymbolEqualityComparer.Default.Equals(from, to) ||
        IsImplicitCharToNumber(operation, from, to) ||
        IsCharInTextConcatenation(operation, from, to);

    static bool IsThrowOrForeachConversion(IConversionOperation operation, ITypeSymbol from, ITypeSymbol to) =>
        (operation.Parent is IThrowOperation && operation.Conversion.IsImplicit && ValidThrow(operation.Operand)) ||
        (operation.Parent is IForEachLoopOperation && from is IArrayTypeSymbol && to.ToDisplayString() == "System.Collections.IEnumerable");

    static bool IsInvariantProviderConversion(IConversionOperation operation, ITypeSymbol to) =>
        to.Name == "IFormatProvider" && IsInvariantProvider(operation.Operand) && operation.Conversion.IsImplicit;

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
