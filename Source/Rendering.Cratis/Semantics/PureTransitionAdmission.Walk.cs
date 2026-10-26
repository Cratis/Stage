// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The operation-by-operation purity walk over a bound reducer or policy body.
/// </summary>
internal static partial class PureTransitionAdmission
{
    internal static Verdict Walk(
        SemanticModel model,
        BlockSyntax body,
        SemanticTypedContextDescriptor descriptor,
        string? rootNamespace = null)
    {
        rootNamespace ??= model.GetDeclaredSymbol((MethodDeclarationSyntax)body.Parent!)!.Parameters[0].Type.ContainingNamespace.ContainingNamespace.ToDisplayString();
        return Walk(model, body, new PureTransitionAnalysisContext(descriptor, rootNamespace, ImmutableDictionary<INamedTypeSymbol, ITypeSymbol>.Empty));
    }

    static Verdict Walk(SemanticModel model, BlockSyntax body, PureTransitionAnalysisContext analysis)
    {
        var root = model.GetOperation(body);
        if (root is null) return Reject("STAGE-ESM-022", "Reducer body has no bound operation.");
        var walker = new Walker(model, root, analysis);
        foreach (var operation in Descendants(root))
        {
            if (walker.Visit(operation) is { } rejection)
            {
                return rejection;
            }
        }

        return walker.Accepted();
    }

    static Verdict Forbid(string reason) => Reject("STAGE-ESM-022", reason);

    static string DateTimeAlternative(INamedTypeSymbol? type) =>
        type?.ToDisplayString() == "System.DateTime" ? " Use DateTimeOffset (context.Occurred) instead." : string.Empty;

    static IOperation? WriteTarget(IOperation operation) => operation switch
    {
        ISimpleAssignmentOperation simple => simple.Target,
        ICompoundAssignmentOperation compound => compound.Target,
        IIncrementOrDecrementOperation increment => increment.Target,
        _ => null
    };

    static string? StaticMember(IOperation target) => target switch
    {
        IFieldReferenceOperation { Field.IsStatic: true } field => field.Field.ToDisplayString(),
        IPropertyReferenceOperation { Property.IsStatic: true } property => property.Property.ToDisplayString(),
        _ => null
    };

    static bool IsEnumerableCall(IInvocationOperation call) =>
        call.TargetMethod.ContainingType.ToDisplayString() == "System.Linq.Enumerable";

    static bool IsRejectedLinq(IInvocationOperation call, IOperation root) =>
        IsEnumerableCall(call) &&
        ((NameIs(call.TargetMethod.Name, "Any", "Count") && call.TargetMethod.Parameters.Length == 1) ||
            call.Arguments.Any(argument => HasDeferredLinqSource(argument.Value, root)));

    static bool IsRejectedImmutable(IInvocationOperation call, IOperation root) =>
        call.TargetMethod.ContainingType.ToDisplayString() == "System.Collections.Immutable.ImmutableArray" &&
        call.Arguments.Any(argument => HasDeferredLinqSource(argument.Value, root));

    static bool IsSafeLocalType(ITypeSymbol type, Compilation compilation) =>
        SafeDataType(type, compilation) ||
        (type is INamedTypeSymbol named &&
            named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>" &&
            SafeDataType(named.TypeArguments[0], compilation));

    static bool IsUnsafeCompound(ICompoundAssignmentOperation compound, Compilation compilation) =>
        !(compound.Type?.SpecialType == SpecialType.System_String && StringOrChar(compound.Value)) &&
        !SafeValue(compound.Value.Type, compilation);

    static bool IsNonStringCompound(ICompoundAssignmentOperation compound) =>
        compound.Type?.SpecialType == SpecialType.System_String &&
        (!StringOrChar(compound.Value) || compound.Target.Type?.SpecialType != SpecialType.System_String);

    static bool IsSafeNumericCast(IConversionOperation conversion) =>
        Number(conversion.Operand.Type) && Number(conversion.Type) &&
        (WideningInteger(conversion.Operand.Type, conversion.Type) ||
            conversion.Operand.Type?.SpecialType == SpecialType.System_Decimal ||
            conversion.Type?.SpecialType == SpecialType.System_Decimal);

    static bool IsUnsafeConversion(IConversionOperation conversion, IReadOnlyDictionary<INamedTypeSymbol, ITypeSymbol> generatedConcepts) =>
        (conversion.Syntax is CastExpressionSyntax && !IsSafeNumericCast(conversion)) || !SafeConversion(conversion, generatedConcepts);

    static bool IsInvariantProviderMisuse(IPropertyReferenceOperation provider) =>
        provider.Property.Name == "InvariantCulture" && IsInvariantProvider(provider) &&
        ArgumentFor(provider) is not IArgumentOperation { Parent: IInvocationOperation { TargetMethod.Name: "ToString" } };

    static bool IsStringConcat(IInvocationOperation call) =>
        call.TargetMethod.Name == "Concat" && call.TargetMethod.ContainingType.SpecialType == SpecialType.System_String;

    static bool IsMutatedElement(IArrayElementReferenceOperation element) =>
        element.Parent is ISimpleAssignmentOperation or ICompoundAssignmentOperation or IIncrementOrDecrementOperation;

    static bool IsNonStringAddition(IBinaryOperation binary) =>
        binary is { OperatorKind: BinaryOperatorKind.Add, Type.SpecialType: SpecialType.System_String } &&
        (!StringOrChar(binary.LeftOperand) || !StringOrChar(binary.RightOperand));

    static bool IsStringAddition(IOperation operation) => operation switch
    {
        ICompoundAssignmentOperation { Type.SpecialType: SpecialType.System_String } compound => StringOrChar(compound.Value),
        IBinaryOperation { Type.SpecialType: SpecialType.System_String, OperatorKind: BinaryOperatorKind.Add } binary =>
            StringOrChar(binary.LeftOperand) && StringOrChar(binary.RightOperand),
        _ => false
    };

    static bool UsesNonInvariantProvider(IInvocationOperation invocation) =>
        invocation.TargetMethod.Parameters.Any(_ => _.Type.Name == "IFormatProvider") &&
        invocation.Arguments.Where(_ => _.Parameter?.Type.Name == "IFormatProvider").Any(argument => !IsInvariantProvider(argument.Value));

    static bool IsUnauditedInvocation(IInvocationOperation call) =>
        !ValidInvocation(call) || (IsStringConcat(call) && call.Arguments.Any(argument => !StringOrChar(argument.Value)));

    static bool IsUnresolvable(IOperation operation, SemanticModel model) => operation switch
    {
        IDeconstructionAssignmentOperation => true,
        IForEachLoopOperation => !IsArrayForeach(operation, model),
        IRecursivePatternOperation { DeconstructionSubpatterns.Length: > 0 } => true,
        _ => false
    };

    static bool IsWrapperWrite(IPropertyReferenceOperation read) =>
        (read.Parent is ISimpleAssignmentOperation { Target: var target } && target == read) ||
        read.Parent is ICompoundAssignmentOperation or IIncrementOrDecrementOperation or ICoalesceAssignmentOperation;

    static bool UnauditedForEachBindings(ForEachStatementInfo info) =>
        info.GetEnumeratorMethod?.ContainingType.ToDisplayString().StartsWith("System.Collections.Immutable.ImmutableArray<", StringComparison.Ordinal) != true ||
        info.MoveNextMethod is null || info.CurrentProperty is null ||
        !AuditedMemberSignatures.Contains(AuditedSignature(info.MoveNextMethod)) ||
        !AuditedMemberSignatures.Contains(AuditedSignature(info.CurrentProperty));

    static IMethodSymbol? OperatorMethod(IOperation operation) => operation switch
    {
        IConversionOperation { OperatorMethod: { } conversion } => conversion,
        IBinaryOperation { OperatorMethod: { } binary } => binary,
        IUnaryOperation { OperatorMethod: { } unary } => unary,
        ICompoundAssignmentOperation { OperatorMethod: { } compound } => compound,
        _ => null
    };

    static ISymbol? BoundSymbol(IOperation operation, SemanticModel model) => operation switch
    {
        IInvocationOperation call => call.TargetMethod,
        IObjectCreationOperation creation => creation.Constructor,
        IPropertyReferenceOperation property => property.Property,
        IFieldReferenceOperation field => field.Field,
        IMethodReferenceOperation methodReference => methodReference.Method,
        _ => SyntaxSymbol(operation, model)
    };

    static ISymbol? SyntaxSymbol(IOperation operation, SemanticModel model) => operation switch
    {
        IForEachLoopOperation loop when loop.Syntax is CommonForEachStatementSyntax syntax =>
            model.GetForEachStatementInfo(syntax).GetEnumeratorMethod,
        IDeconstructionAssignmentOperation { Syntax: AssignmentExpressionSyntax deconstruction } =>
            model.GetDeconstructionInfo(deconstruction).Method,
        IRecursivePatternOperation recursive => recursive.DeconstructSymbol,
        _ => OperatorMethod(operation)
    };

    sealed class Walker(SemanticModel model, IOperation root, PureTransitionAnalysisContext analysis)
    {
        readonly ImmutableHashSet<string>.Builder _reads = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        readonly ImmutableHashSet<string>.Builder _used = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        readonly Lazy<string> _wrapper = new(() => $"TypedContext_{SemanticTypedContextRenderer.Suffix(analysis.Descriptor)}");

        string Wrapper => _wrapper.Value;

        Compilation Compilation => model.Compilation;

        bool IsValidationIterator => analysis.Descriptor.Role is SemanticImplementationRole.CommandValidation or SemanticImplementationRole.ConceptValidation;

        public Verdict Accepted() => new(
            null,
            null,
            _reads.ToImmutable(),
            _used.ToImmutable(),
            [.. Descendants(root).SelectMany(operation => BoundMembers(operation, model)).Select(SymbolIdentity)],
            [.. Descendants(root).SelectMany(operation => BoundMembers(operation, model)).Where(IsAuditedMember).Select(AuditedSignature).Distinct(StringComparer.Ordinal)]);

        public Verdict? Visit(IOperation operation) =>
            InterpolationRejection(operation) ?? TypeRejection(operation) ?? StructureRejection(operation) ?? SymbolRejection(operation);

        static Verdict? InterpolationRejection(IOperation operation) =>
            operation is IInterpolationOperation hole && !SafeInterpolationType(hole.Expression.Type)
                ? Forbid($"Interpolation of '{hole.Expression.Type?.ToDisplayString()}' is outside the pure allowlist.")
                : null;

        static bool SafeInterpolationType(ITypeSymbol? type) =>
            SafeValueType(type) || NameIs(type?.ToDisplayString() ?? string.Empty, "System.DateTimeOffset", "System.TimeSpan");

        static Verdict? EnumFieldRejection(IOperation operation) => operation switch
        {
            IFieldReferenceOperation enumField when IsUnauditedEnumField(enumField) =>
                Forbid($"Enum field '{enumField.Field.ToDisplayString()}' is admitted only as an audited method argument."),
            _ => null
        };

        static bool IsUnauditedEnumField(IFieldReferenceOperation field) =>
            NameIs(field.Field.ContainingType.ToDisplayString(), "System.StringComparison", "System.MidpointRounding") && !AuditedEnumArgument(field);

        static Verdict? StringRejection(IOperation operation) => operation switch
        {
            ICompoundAssignmentOperation compound when IsNonStringCompound(compound) =>
                Forbid("Culture-sensitive string.Concat operands are not pure."),
            IInvocationOperation concat when IsStringConcat(concat) && concat.Arguments.Any(argument => !StringOrChar(argument.Value)) =>
                Forbid("Symbol 'string.Concat' requires string or char operands."),
            _ => null
        };

        static Verdict? ForbiddenRejection(IOperation operation) =>
            DynamicRejection(operation) ?? ExceptionRejection(operation);

        static Verdict? DynamicRejection(IOperation operation) => operation switch
        {
            IDynamicInvocationOperation or IDynamicMemberReferenceOperation or IDynamicObjectCreationOperation or IDynamicIndexerAccessOperation =>
                Forbid($"Forbidden pure operation '{operation.Kind}'."),
            _ when operation.Type?.TypeKind is TypeKind.Dynamic or TypeKind.Pointer => Forbid($"Forbidden pure operation '{operation.Kind}'."),
            _ => null
        };

        static Verdict? ExceptionRejection(IOperation operation) => operation switch
        {
            IObjectCreationOperation constructed when IsExceptionType(constructed) && !IsThrownCreation(constructed) =>
                Forbid("Exception construction is admitted only inside a throw expression."),
            IThrowOperation thrown when !ValidThrow(thrown.Exception) =>
                Forbid("Throw must construct InvalidOperationException(string) or ArgumentException(string)."),
            _ => null
        };

        static bool IsExceptionType(IObjectCreationOperation creation) => NameIs(
            creation.Constructor?.ContainingType.ToDisplayString() ?? string.Empty,
            "System.InvalidOperationException",
            "System.ArgumentException");

        static string? ValueTypeName(IOperation operation) => operation switch
        {
            IIsPatternOperation pattern => pattern.Value.Type?.Name,
            IWithOperation withOperation => withOperation.Operand.Type?.Name,
            IDeconstructionAssignmentOperation deconstruction => deconstruction.Value.Type?.Name,
            IArgumentOperation argument => argument.Value.Type?.Name,
            _ => null
        };

        static Verdict? StaticWriteRejection(IOperation operation)
        {
            var target = WriteTarget(operation);
            return target is not null && StaticMember(target) is { } member
                ? Forbid($"Write to static member '{member}' is not pure.")
                : null;
        }

        static Verdict? ProviderRejection(IOperation operation) => operation switch
        {
            IInvocationOperation invocation when UsesNonInvariantProvider(invocation) =>
                Forbid($"Symbol '{invocation.TargetMethod.ToDisplayString()}' requires CultureInfo.InvariantCulture."),
            _ => null
        };

        // Only the named, audited operation shapes are admitted. Unknown Roslyn kinds fail closed.
        Verdict? KindRejection(IOperation operation) =>
            _admittedOperations.Contains(operation.Kind) ||
            (IsValidationIterator && operation.Kind is OperationKind.YieldReturn or OperationKind.InterpolatedString or OperationKind.InterpolatedStringText or OperationKind.Interpolation)
                ? null
                : Forbid($"Operation '{operation.Kind}' on '{operation.Type?.ToDisplayString()}' is outside the pure allowlist.");

        Verdict? TypeRejection(IOperation operation) =>
            ForbiddenRejection(operation) ??
            LinqRejection(operation) ??
            LocalRejection(operation) ??
            OperatorRejection(operation) ??
            MemberRejection(operation);

        Verdict? StructureRejection(IOperation operation) =>
            ConversionRejection(operation) ??
            ConstructionRejection(operation) ??
            StringRejection(operation) ??
            UsageRejection(operation);

        Verdict? UsageRejection(IOperation operation) =>
            StaticWriteRejection(operation) ??
            KindRejection(operation) ??
            WrapperRejection(operation) ??
            ProviderRejection(operation);

        Verdict? LinqRejection(IOperation operation) => operation switch
        {
            IAnonymousFunctionOperation lambda when InvalidLambda(lambda) =>
                Forbid("LINQ lambda must be a pure expression over its parameters and typed context (no writes, deferred captures, nested LINQ or evaluation-dependent throws)."),
            IInvocationOperation linq when IsRejectedLinq(linq, root) =>
                Forbid("LINQ count shortcuts and deferred LINQ composition are outside the pure subset."),
            IInvocationOperation immutable when IsRejectedImmutable(immutable, root) =>
                Forbid("Materializing a deferred LINQ source is outside the pure subset."),
            _ => null
        };

        Verdict? LocalRejection(IOperation operation) => operation switch
        {
            ISimpleAssignmentOperation { Target: ILocalReferenceOperation assigned } when EnumerableType(assigned.Local.Type) =>
                Forbid($"Enumerable local '{assigned.Local.Name}' must be single-assignment."),
            ILiteralOperation { Type: { } literalType } when !SafeDataType(literalType, Compilation) =>
                Forbid($"Literal of '{literalType}' is outside the pure allowlist."),
            IVariableDeclaratorOperation declarator when !IsSafeLocalType(declarator.Symbol.Type, Compilation) =>
                Forbid($"Local '{declarator.Symbol.Name}' has a type outside the pure allowlist."),
            _ => null
        };

        Verdict? OperatorRejection(IOperation operation) => operation switch
        {
            IBinaryOperation binary when !SafeBinary(binary, Compilation) =>
                Forbid($"Binary operation '{binary.OperatorKind}' on '{binary.LeftOperand.Type}' is outside the pure allowlist.{DateTimeAlternative(binary.OperatorMethod?.ContainingType)}"),
            IUnaryOperation unary when !SafeValue(unary.Operand.Type, Compilation) =>
                Forbid($"Unary operation on '{unary.Operand.Type}' is outside the pure allowlist."),
            ICompoundAssignmentOperation compound when IsUnsafeCompound(compound, Compilation) =>
                Forbid($"Compound assignment on '{compound.Value.Type}' is outside the pure allowlist."),
            IIncrementOrDecrementOperation increment when !Number(increment.Target.Type) =>
                Forbid($"Increment on '{increment.Target.Type}' is outside the pure allowlist."),
            _ => null
        };

        Verdict? MemberRejection(IOperation operation) =>
            EnumFieldRejection(operation) ?? PropertyRejection(operation);

        Verdict? PropertyRejection(IOperation operation) => operation switch
        {
            IPropertyReferenceOperation provider when IsInvariantProviderMisuse(provider) =>
                Forbid("CultureInfo.InvariantCulture is admitted only for numeric ToString."),
            IPropertyReferenceOperation value when IsUnsafePropertyType(value) =>
                Forbid($"Property '{value.Property.ToDisplayString()}' returns a type outside the pure allowlist.{DateTimeAlternative(value.Property.ContainingType)}"),
            _ => null
        };

        bool IsUnsafePropertyType(IPropertyReferenceOperation value) =>
            !SafeDataType(value.Type, Compilation) && !(value.Property.Name == "InvariantCulture" && IsInvariantProvider(value));

        Verdict? ConversionRejection(IOperation operation) => operation switch
        {
            IConversionOperation conversion when IsUnsafeConversion(conversion, analysis.GeneratedConcepts) =>
                Forbid($"Conversion to '{conversion.Type?.ToDisplayString()}' is outside the pure allowlist."),
            IIsPatternOperation pattern when !SafePattern(pattern.Pattern, Compilation) =>
                Forbid($"Pattern '{pattern.Pattern.Syntax}' is outside the pure allowlist."),
            IDefaultValueOperation defaultValue when !Generated(defaultValue.Type, Compilation) =>
                Forbid($"Default of '{defaultValue.Type?.ToDisplayString()}' is outside the pure allowlist."),
            _ => null
        };

        Verdict? ConstructionRejection(IOperation operation) => operation switch
        {
            IArrayCreationOperation { Type: IArrayTypeSymbol arrayType } when !SafeValue(arrayType.ElementType, Compilation) =>
                Forbid($"Array element type '{arrayType.ElementType}' is outside the pure allowlist."),
            IWithOperation withOperation when !Generated(withOperation.Type, Compilation) =>
                Forbid($"With on '{withOperation.Type?.ToDisplayString()}' is outside the pure allowlist."),
            IArrayElementReferenceOperation element when IsMutatedElement(element) =>
                Forbid("Array element mutation is outside the pure allowlist."),
            _ => null
        };

        Verdict? WrapperRejection(IOperation operation)
        {
            if (IsStrayWrapperReference(operation) || IsWrapperValue(operation))
            {
                return Forbid($"Wrapper '{Wrapper}' may only be used through a property getter.");
            }

            return operation is IBinaryOperation binary && IsNonStringAddition(binary)
                ? Forbid("Culture-sensitive string.Concat operands are not pure.")
                : null;
        }

        bool IsStrayWrapperReference(IOperation operation) =>
            operation is IParameterReferenceOperation or ILocalReferenceOperation &&
            operation.Type?.Name == Wrapper &&
            !(operation.Parent is IPropertyReferenceOperation { Instance: { } instance } && instance == operation);

        bool IsWrapperValue(IOperation operation) => ValueTypeName(operation) == Wrapper;

        Verdict? SymbolRejection(IOperation operation) =>
            BoundSymbol(operation, model) is { } symbol ? BoundRejection(operation, symbol) : UnresolvedRejection(operation);

        Verdict? UnresolvedRejection(IOperation operation) =>
            IsUnresolvable(operation, model)
                ? Forbid($"Unresolved operation '{operation.Kind}' is outside the pure allowlist.")
                : null;

        Verdict? BoundRejection(IOperation operation, ISymbol symbol)
        {
            if (IsArrayForeach(operation, model))
            {
                _used.Add("collections");
                return null;
            }

            if (ForEachRejection(operation) is { } rejection)
            {
                return rejection;
            }

            if (IsStringAddition(operation))
            {
                _used.Add("string");
                return null;
            }

            return AdmissionRejection(operation, symbol);
        }

        Verdict? ForEachRejection(IOperation operation) =>
            operation is IForEachLoopOperation { Syntax: CommonForEachStatementSyntax forEach } &&
            UnauditedForEachBindings(model.GetForEachStatementInfo(forEach))
                ? Forbid("Foreach requires audited immutable-array GetEnumerator, MoveNext and Current bindings.")
                : null;

        Verdict? AdmissionRejection(IOperation operation, ISymbol symbol)
        {
            if (operation is IInvocationOperation invoked && IsUnauditedInvocation(invoked))
            {
                return Forbid($"Symbol '{symbol.ToDisplayString()}' has unaudited arguments.");
            }

            if (WrapperReadRejection(operation) is { } rejection)
            {
                return rejection;
            }

            if (!IsAdmitted(operation, symbol, out var entry))
            {
                return Forbid($"Symbol '{symbol.ToDisplayString()}' is outside the pure allowlist.{DateTimeAlternative(symbol.ContainingType)}");
            }

            if (IsAuditedMember(symbol) && !AuditedMemberSignatures.Contains(AuditedSignature(symbol)))
            {
                return Forbid($"Symbol '{symbol.ToDisplayString()}' has no audited signature fixture.");
            }

            _used.Add(entry);
            return null;
        }

        bool IsAdmitted(IOperation operation, ISymbol symbol, out string entry) =>
            Allowed(symbol, Compilation, analysis, out entry) &&
            !(symbol.ContainingType?.Name == Wrapper && operation is not IPropertyReferenceOperation);

        Verdict? WrapperReadRejection(IOperation operation)
        {
            if (operation is not IPropertyReferenceOperation read || read.Property.ContainingType.Name != Wrapper)
            {
                return null;
            }

            if (IsWrapperWrite(read))
            {
                return Forbid($"Writing wrapper property '{read.Property.ToDisplayString()}' is not pure.");
            }

            _reads.Add(read.Property.Name);
            return null;
        }
    }
}
