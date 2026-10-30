// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Semantics.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class PureTransitionAdmission
{
    private sealed partial class Walker
    {
        static bool IsDigitPredicate(IOperation operation) => operation is IMethodReferenceOperation reference &&
            IsDigitMember(reference.Method) && reference.Parent is IDelegateCreationOperation { Parent: IArgumentOperation { Parent: IInvocationOperation all } } &&
            all.TargetMethod.Name == "All";

        static bool HasValidationComparison(IInvocationOperation call) =>
            call.TargetMethod.ContainingType.SpecialType != SpecialType.System_String ||
            !call.TargetMethod.Parameters.Any(parameter => parameter.Type.ToDisplayString() == "System.StringComparison") || HasOrdinalComparison(call);

        static bool ValidationMethod(IMethodSymbol method) => method.ContainingType.SpecialType switch
        {
            SpecialType.System_Char => IsDigitMember(method),
            SpecialType.System_String => ValidationStringMethod(method),
            _ => ValidationValueMethod(method)
        };

        static bool ValidationStringMethod(IMethodSymbol method) =>
            method.Name == "IsNullOrWhiteSpace" ||
            (NameIs(method.Name, "Contains", "StartsWith", "EndsWith") && ValidationStringParameters(method));

        static bool ValidationStringParameters(IMethodSymbol method) => method.Parameters is
            [{ Type.SpecialType: SpecialType.System_String or SpecialType.System_Char }] or
            [{ Type.SpecialType: SpecialType.System_String }, { Type.Name: "StringComparison" }];

        static bool ValidationValueMethod(IMethodSymbol method)
        {
            var owner = method.ContainingType.ToDisplayString();
            if (owner == "System.Linq.Enumerable") return method.Name == "Sum" && method.Parameters.Length == 1 && PureValidationAdmission.WholeNumber(method.ReturnType);
            return NameIs(owner, "System.Guid", "System.DateOnly") && ValidationComparison(method);
        }

        static bool ValidationComparison(IMethodSymbol method) =>
            NameIs(method.Name, "op_Equality", "op_Inequality", "op_LessThan", "op_LessThanOrEqual", "op_GreaterThan", "op_GreaterThanOrEqual") &&
            method.Parameters.All(parameter => SymbolEqualityComparer.Default.Equals(parameter.Type, method.ContainingType));

        bool ValidationPattern(IIsPatternOperation pattern) => IsValidation &&
            pattern.Pattern is IDeclarationPatternOperation declaration && SafeDataType(declaration.MatchedType, Compilation);

        bool AdmittedValidationKind(IOperation operation) =>
            IsValidation && (operation.Kind is OperationKind.IsType or OperationKind.TypePattern || IsDigitPredicate(operation) || IsIteratorOperation(operation));

        bool IsIteratorOperation(IOperation operation) => IsValidationIterator && operation.Kind is
            OperationKind.YieldReturn or OperationKind.InterpolatedString or OperationKind.InterpolatedStringText or OperationKind.Interpolation;

        bool IsValidationAccessor(IOperation operation, ISymbol symbol) =>
            IsValidation && symbol is IMethodSymbol accessor && accessor.ContainingType.Name == Wrapper &&
            NameIs(accessor.Name, "ValueAs", "ArtifactAs") && operation is IInvocationOperation;

        bool AdmittedValidationOperation(IOperation operation, ISymbol symbol) =>
            IsValidation && (ValidationMember(symbol) || (operation is IConversionOperation conversion && ValidationConversion(conversion)));

        bool ValidationConversion(IConversionOperation conversion)
        {
            if (ValidationValueRead(conversion.Operand) is { } read)
                return PureValidationAdmission.ValueTarget(read.Type!, conversion.Type, concept: IsConceptValue);
            return conversion.IsImplicit && conversion.Operand.Type?.SpecialType == SpecialType.System_String &&
                conversion.Type?.ToDisplayString() == "System.Collections.Generic.IEnumerable<char>";
        }

        IPropertyReferenceOperation? ValidationValueRead(IOperation operand) =>
            operand is IPropertyReferenceOperation read && read.Property.Name == "Value" && read.Property.ContainingType.Name == Wrapper ? read : null;

        bool ValidationInvocation(IInvocationOperation call) =>
            IsValidation && ValidationMember(call.TargetMethod) && HasValidationComparison(call);

        bool ValidationMember(ISymbol symbol)
        {
            if (!IsValidation) return false;
            if (symbol is IFieldSymbol field) return field.ContainingType.ToDisplayString() == "System.StringComparison" && field.Name == "OrdinalIgnoreCase";
            return symbol is IMethodSymbol method && ValidationMethod(method);
        }
    }
}
