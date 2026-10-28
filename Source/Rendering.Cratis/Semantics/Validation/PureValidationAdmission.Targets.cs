// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Validation;

internal static partial class PureValidationAdmission
{
    internal static bool ValueTarget(ITypeSymbol from, ITypeSymbol? target, bool typeTest = false, bool concept = false)
    {
        if (SymbolEqualityComparer.Default.Equals(from, target)) return true;
        if (typeTest) return false;
        var underlying = UnwrapNullable(from);
        var destination = UnwrapNullable(target);
        return SameRealization(underlying, destination) ||
            (concept && ConceptValueTarget(underlying, destination)) || SequenceTarget(from, target);
    }

    internal static bool WholeNumber(ITypeSymbol type) => type.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int64;

    static ITypeSymbol? UnwrapNullable(ITypeSymbol? type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } optional ? optional.TypeArguments[0] : type;

    static bool SameRealization(ITypeSymbol? from, ITypeSymbol? to) =>
        SymbolEqualityComparer.Default.Equals(from, to) || (from is not null && to is not null && WholeNumber(from) && WholeNumber(to));

    static bool ConceptValueTarget(ITypeSymbol? underlying, ITypeSymbol? destination) =>
        underlying is INamedTypeSymbol nominal && nominal.GetMembers("Value").OfType<IPropertySymbol>().FirstOrDefault() is { } value &&
        ValueTarget(value.Type, destination);

    static bool SequenceTarget(ITypeSymbol from, ITypeSymbol? target)
    {
        if (target is not INamedTypeSymbol { IsGenericType: true } sequence || !IsSequence(sequence)) return false;
        var element = from.AllInterfaces.Prepend(from).OfType<INamedTypeSymbol>().FirstOrDefault(IsSequence)?.TypeArguments[0];
        return element is not null && SameRealization(element, sequence.TypeArguments[0]);
    }

    static bool IsSequence(INamedTypeSymbol type) => type.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>";

    static PureTransitionAdmission.Verdict? TargetRejection(MethodDeclarationSyntax method, SemanticModel model, SemanticTypedContextDescriptor descriptor)
    {
        var concept = descriptor.Members.Single(member => member.Name == "Value").Type.ModelType?.Kind == SemanticTypeReferenceKind.Concept;
        foreach (var node in method.Body!.DescendantNodes())
        {
            var operation = model.GetOperation(node);
            if (operation is IInvocationOperation call && IsAccessor(call))
            {
                if (AccessorRejection(call, concept) is { } rejection) return rejection;
            }
            else if (CastOrTest(node, operation, model) is { } conversion && ConversionRejection(conversion, concept) is { } rejection)
            {
                return rejection;
            }
        }
        return null;
    }

    static bool IsAccessor(IInvocationOperation call) => IsWrapper(call.TargetMethod.ContainingType) &&
        (call.TargetMethod.Name == "ValueAs" || call.TargetMethod.Name == "ArtifactAs");

    static bool IsWrapper(INamedTypeSymbol type) => type.Name.StartsWith("TypedContext_", StringComparison.Ordinal);

    static PureTransitionAdmission.Verdict? AccessorRejection(IInvocationOperation call, bool concept)
    {
        var member = call.TargetMethod.Name == "ValueAs" ? "Value" : "Artifact";
        var from = call.TargetMethod.ContainingType.GetMembers(member).OfType<IPropertySymbol>().Single().Type;
        var to = call.TargetMethod.TypeArguments.Single();
        var admitted = member == "Value" ? ValueTarget(from, to, concept: concept) : SymbolEqualityComparer.Default.Equals(from, to);
        return admitted ? null : Reject("STAGE-ESM-022", $"Accessor target '{to}' is outside the declared validation realization.");
    }

    static ValueConversion? CastOrTest(SyntaxNode node, IOperation? operation, SemanticModel model) => node switch
    {
        CastExpressionSyntax cast => new(model.GetOperation(cast.Expression), model.GetTypeInfo(cast.Type).Type, false),
        _ when operation is IIsTypeOperation test => new(test.ValueOperand, test.TypeOperand, true),
        _ when operation is IIsPatternOperation { Pattern: ITypePatternOperation typed } pattern => new(pattern.Value, typed.MatchedType, true),
        _ => null
    };

    static PureTransitionAdmission.Verdict? ConversionRejection(ValueConversion conversion, bool concept)
    {
        if (AlwaysTrueTypeTest(conversion))
        {
            return Reject("STAGE-ESM-022", $"Type test 'is {conversion.Target}' on a non-nullable '{conversion.Target}' value is always true; use the value directly or a designation pattern.");
        }
        return AdmittedTarget(conversion, concept) ? null :
            Reject("STAGE-ESM-022", $"Validation cast or type test to '{conversion.Target}' is outside the declared value realization.");
    }

    static bool AlwaysTrueTypeTest(ValueConversion conversion) => conversion.TypeTest &&
        conversion.Operand?.Type is { IsValueType: true } from && from.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T &&
        SymbolEqualityComparer.Default.Equals(from, conversion.Target);

    static bool AdmittedTarget(ValueConversion conversion, bool concept) =>
        conversion.Operand is IPropertyReferenceOperation read && read.Property.Name == "Value" && IsWrapper(read.Property.ContainingType) &&
        ValueTarget(read.Type!, conversion.Target, conversion.TypeTest, concept);

    sealed record ValueConversion(IOperation? Operand, ITypeSymbol? Target, bool TypeTest);
}
