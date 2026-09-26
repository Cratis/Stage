// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Closed-compilation, fail-closed symbol admission for attached pure transitions (not a sandbox).</summary>
internal static class PureTransitionAdmission
{
    // Each entry is a separate exemption; a fixture must exercise it and any new side effect ends the exemption.
    internal static readonly IReadOnlyDictionary<string, string> AllowlistReasons = new Dictionary<string, string>
    {
        ["generated"] = "Source-declared Stage-owned records, wrappers and concept value members; no anonymous types or generated hash methods.",
        ["primitive"] = "CLR primitive arithmetic and immutable values; remove if a primitive member consults ambient state.",
        ["math"] = "Deterministic numeric operations; remove if Math grows stateful operations.",
        ["time"] = "Value arithmetic, never Now/UtcNow/Today; remove if a member consults ambient time.",
        ["guid"] = "Guid value operations, never NewGuid/CreateVersion7; remove if a member consults randomness.",
        ["string"] = "Ordinal and provider-explicit text operations only; remove if culture-independent behaviour changes.",
        ["collections"] = "Explicit list and indexed collection members only; hash-ordered types are refused entirely because their iteration order depends on process-randomized string hashes.",
        ["linq"] = "Explicit order-preserving Enumerable operators only; randomness and comparer-default sorting are refused.",
        ["exceptions"] = "System exception construction for explicit throw; remove if constructors acquire side effects."
    };

    // The exact scaffold profile pins the target TFM; never analyze against Stage's implementation CoreLib.
    // If the corresponding reference pack is unavailable we refuse the body instead of falling back to another runtime.
    internal static readonly Lazy<MetadataReference[]> _references = new(() =>
    {
        var framework = CratisBackendApplicationScaffoldProfile.Current.TargetFramework;
        var dotnetRoot = new DirectoryInfo(Path.GetDirectoryName(typeof(object).Assembly.Location)!).Parent!.Parent!.Parent!;
        var packRoot = Path.Combine(dotnetRoot.FullName, "packs", "Microsoft.NETCore.App.Ref");
        var version = Directory.Exists(packRoot) ? Directory.GetDirectories(packRoot)
            .Select(Path.GetFileName)
            .Where(name => Version.TryParse(name, out var parsed) && parsed.Major.ToString(System.Globalization.CultureInfo.InvariantCulture) == framework[3..framework.IndexOf('.', StringComparison.Ordinal)])
            .OrderByDescending(name => Version.Parse(name!))
            .FirstOrDefault() : null;
        var refDirectory = Path.Combine(packRoot, version ?? string.Empty, "ref", framework);
        if (version is null || !Directory.Exists(refDirectory))
            throw new InvalidOperationException($"Reference assemblies for target framework '{framework}' are unavailable; pure transitions cannot be analysed.");
        return [.. Directory.GetFiles(refDirectory, "*.dll").Select(path => MetadataReference.CreateFromFile(path))];
    });

    static readonly HashSet<OperationKind> _admittedOperations =
    [
        OperationKind.Block, OperationKind.Return, OperationKind.Throw, OperationKind.ExpressionStatement,
        OperationKind.VariableDeclarationGroup, OperationKind.VariableDeclaration, OperationKind.VariableDeclarator,
        OperationKind.VariableInitializer, OperationKind.LocalReference, OperationKind.ParameterReference,
        OperationKind.Literal, OperationKind.DefaultValue, OperationKind.TypeOf, OperationKind.NameOf,
        OperationKind.Invocation, OperationKind.ObjectCreation, OperationKind.AnonymousFunction,
        OperationKind.LocalFunction, OperationKind.Argument, OperationKind.Conversion, OperationKind.Binary,
        OperationKind.Unary, OperationKind.Conditional, OperationKind.Coalesce, OperationKind.ConditionalAccess,
        OperationKind.ConditionalAccessInstance, OperationKind.PropertyReference, OperationKind.FieldReference,
        OperationKind.MethodReference, OperationKind.InstanceReference, OperationKind.SimpleAssignment,
        OperationKind.CompoundAssignment, OperationKind.Increment, OperationKind.Decrement,
        OperationKind.ArrayCreation, OperationKind.ArrayInitializer, OperationKind.ArrayElementReference,
        OperationKind.ObjectOrCollectionInitializer,
        OperationKind.IsPattern, OperationKind.DeclarationPattern, OperationKind.ConstantPattern,
        OperationKind.RecursivePattern, OperationKind.DiscardPattern, OperationKind.RelationalPattern,
        OperationKind.BinaryPattern, OperationKind.NegatedPattern, OperationKind.ListPattern,
        OperationKind.DeconstructionAssignment, OperationKind.Tuple, OperationKind.Loop,
        OperationKind.Branch, OperationKind.Empty,
        OperationKind.Switch, OperationKind.SwitchCase, OperationKind.CaseClause,
        OperationKind.SwitchExpression, OperationKind.SwitchExpressionArm, OperationKind.Discard,
        OperationKind.InterpolatedStringText
    ];

    internal static Verdict Analyze(
        string body,
        SemanticApplicationContext context,
        SemanticReadModel readModel,
        SemanticEventContract @event,
        SemanticTypedContextDescriptor descriptor,
        SemanticImplementationRequirement requirement)
    {
        var text = "{\n" + body + "\n}";
        var statement = SyntaxFactory.ParseStatement(text, consumeFullText: true);
        if (statement.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsDirective))
        {
            return Reject("STAGE-ESM-022", "directive");
        }

        if (statement is not BlockSyntax block || block.ContainsDiagnostics || block.ContainsSkippedText ||
            block.CloseBraceToken.IsMissing || block.CloseBraceToken.SpanStart != text.Length - 1)
        {
            return Reject("STAGE-ESM-019", "The reducer body is not a complete C# statement block.");
        }

        var forbidden = block.DescendantNodesAndSelf().FirstOrDefault(node =>
            node is AwaitExpressionSyntax or YieldStatementSyntax or LockStatementSyntax or UnsafeStatementSyntax or
                FixedStatementSyntax or StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax or
                PointerTypeSyntax or FunctionPointerTypeSyntax or InterpolatedStringExpressionSyntax or
                AnonymousFunctionExpressionSyntax { AsyncKeyword.RawKind: not 0 } or
                LocalFunctionStatementSyntax { AttributeLists.Count: > 0 } or
                ParenthesizedLambdaExpressionSyntax { AttributeLists.Count: > 0 } or
                SimpleLambdaExpressionSyntax { AttributeLists.Count: > 0 } or
                ParameterSyntax { AttributeLists.Count: > 0 } ||
            (node is LocalFunctionStatementSyntax local &&
                (local.Modifiers.Any(SyntaxKind.AsyncKeyword) || local.Modifiers.Any(SyntaxKind.ExternKeyword))));
        if (forbidden is not null)
        {
            var symbol = forbidden switch
            {
                LocalFunctionStatementSyntax local => local.Identifier.Text,
                ParameterSyntax parameter => parameter.Identifier.Text,
                AnonymousFunctionExpressionSyntax => "lambda",
                _ => forbidden.Kind().ToString()
            };
            return Reject("STAGE-ESM-022", $"Forbidden pure construct '{forbidden.Kind()}' on '{symbol}'.");
        }

        var modelNs = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(readModel.Id).Path);
        var ns = SliceNaming.Namespace(context.RootNamespace, context.SelectedSlices().Single(located => located.Slice.Reducers.Any(reducer =>
            reducer.ReadModel == readModel.Id)).Path);
        var eventNs = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path);
        var types = new SemanticTypeSystem(context);
        var definitions = new List<string>();
        foreach (var concept in context.Application.Concepts)
        {
            if (!concept.Values.IsEmpty)
            {
                definitions.Add($"namespace {context.RootNamespace}.Common {{ public enum {Identifiers.ToPascalCase(concept.Name)} {{ {string.Join(", ", concept.Values.Select(Identifiers.ToPascalCase))} }} }}");
                continue;
            }

            var scalar = SemanticTypeSystem.Primitive(concept.Primitive);
            definitions.Add($"namespace {context.RootNamespace}.Common {{ public record {Identifiers.ToPascalCase(concept.Name)}({scalar} Value) {{ public static implicit operator {scalar}({Identifiers.ToPascalCase(concept.Name)} value) => value.Value; public static implicit operator {Identifiers.ToPascalCase(concept.Name)}({scalar} value) => new(value); }} }}");
        }

        foreach (var composite in context.Application.Types)
        {
            definitions.Add($"namespace {context.RootNamespace}.Common {{ public record {Identifiers.ToPascalCase(composite.Name)}({Parameters(composite.Properties)}); }}");
        }

        definitions.Add($"namespace {modelNs} {{ public record {Identifiers.ToPascalCase(readModel.Name)}({Parameters(readModel.Properties)}); }}");
        definitions.Add($"namespace {eventNs} {{ public record {Identifiers.ToPascalCase(@event.Name)}({Parameters(@event.Properties)}); }}");
        var wrapper = SemanticTypedContextRenderer.Render(descriptor, context).Content
            .Replace($"namespace {context.RootNamespace}.TypedContexts;", $"namespace {context.RootNamespace}.TypedContexts {{", StringComparison.Ordinal) + "\n}";
        var modelType = $"global::{modelNs}.{Identifiers.ToPascalCase(readModel.Name)}";
        var wrapperType = $"global::{context.RootNamespace}.TypedContexts.TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}";
        var tenant = $"namespace {context.RootNamespace}.TypedContexts {{ public record TenantId(string Value); }}";

        // Model stubs are compilation peers, not imports into the reducer file. The body has the
        // same namespace and exact using directives as SemanticReducerArtifactRenderer.Render.
        var declarations = "global using System; global using System.Collections.Generic; global using System.Linq; " +
            "global using System.IO; global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks; " +
            (context.Application.Concepts.IsEmpty && context.Application.Types.IsEmpty ? string.Empty : $"using {context.RootNamespace}.Common; ") +
            "namespace Cratis.Chronicle.Events {} namespace Cratis.Chronicle.Reducers {} " +
            string.Join('\n', definitions) + "\n" + tenant + "\n" + wrapper;
        var code = new CodeGeneration.CSharpCodeBuilder()
            .Namespace(ns)
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Reducers")
            .Using($"{context.RootNamespace}.TypedContexts")
            .Using(modelNs)
            .Using(eventNs)
            .OpenBlock("public static class TransitionAnalysis")
            .OpenBlock($"public static {modelType}? Transition({wrapperType} context)")
            .RawVerbatim(body)
            .EndBlock()
            .EndBlock()
            .ToString();
        var options = new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest);
        var tree = CSharpSyntaxTree.ParseText(code, options);
        var declarationTree = CSharpSyntaxTree.ParseText(declarations, options);
        var compilation = CSharpCompilation.Create(
            "PureTransitionAnalysis",
            [declarationTree, tree],
            _references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            var error = errors[0];
            var methodOffset = code.IndexOf(body, code.LastIndexOf("public static class TransitionAnalysis", StringComparison.Ordinal), StringComparison.Ordinal);
            var detail = $"{error.Id}: {error.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}";
            if (!error.Location.IsInSource || error.Location.SourceTree != tree || error.Location.SourceSpan.Start < methodOffset ||
                error.Location.SourceSpan.Start > methodOffset + body.Length)
            {
                return Reject("STAGE-ESM-021", $"Internal reducer analysis compilation error: {detail}");
            }

            var location = SourceLocation(requirement, body, error.Location.SourceSpan.Start - methodOffset);
            return Reject("STAGE-ESM-019", $"Reducer body {location}: {detail}");
        }

        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(_ => _.Identifier.Text == "Transition");
        return Walk(compilation.GetSemanticModel(tree), method.Body!, descriptor);

        string Parameters(IEnumerable<SemanticProperty> properties) => string.Join(", ", properties.Select(property =>
            $"{types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}"));
    }

    internal static Verdict AnalyzeRendered(CSharpCompilation compilation, string reducerPath, SemanticTypedContextDescriptor descriptor)
    {
        var tree = compilation.SyntaxTrees.Single(_ => _.FilePath == reducerPath);
        var methodName = "Transition_" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(descriptor.RequirementId)))[..8];
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(_ => _.Identifier.Text == methodName);
        if (compilation.GetDiagnostics().Any(_ => _.Severity == DiagnosticSeverity.Error))
        {
            return Reject("STAGE-ESM-019", "The rendered application does not compile.");
        }

        return Walk(compilation.GetSemanticModel(tree), method.Body!, descriptor);
    }

    static Verdict Walk(SemanticModel model, BlockSyntax body, SemanticTypedContextDescriptor descriptor)
    {
        var root = model.GetOperation(body);
        if (root is null) return Reject("STAGE-ESM-022", "Reducer body has no bound operation.");
        var reads = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        var used = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (var operation in Descendants(root))
        {
            if (operation is IDynamicInvocationOperation or IDynamicMemberReferenceOperation or IDynamicObjectCreationOperation or IDynamicIndexerAccessOperation ||
                operation.Type?.TypeKind is TypeKind.Dynamic or TypeKind.Pointer)
            {
                return Reject("STAGE-ESM-022", $"Forbidden pure operation '{operation.Kind}'.");
            }

            if ((operation is ISimpleAssignmentOperation assignment && assignment.Target is IFieldReferenceOperation { Field.IsStatic: true } or
                IPropertyReferenceOperation { Property.IsStatic: true }) ||
                (operation is ICompoundAssignmentOperation compound && compound.Target is IFieldReferenceOperation { Field.IsStatic: true } or
                    IPropertyReferenceOperation { Property.IsStatic: true }) ||
                (operation is IIncrementOrDecrementOperation increment && increment.Target is IFieldReferenceOperation { Field.IsStatic: true } or
                    IPropertyReferenceOperation { Property.IsStatic: true }))
            {
                var target = operation switch
                {
                    ISimpleAssignmentOperation simple => simple.Target,
                    ICompoundAssignmentOperation compoundAssignment => compoundAssignment.Target,
                    IIncrementOrDecrementOperation incrementOperation => incrementOperation.Target,
                    _ => operation
                };
                var member = target switch
                {
                    IFieldReferenceOperation field => field.Field.ToDisplayString(),
                    IPropertyReferenceOperation property => property.Property.ToDisplayString(),
                    _ => target.Kind.ToString()
                };
                return Reject("STAGE-ESM-022", $"Write to static member '{member}' is not pure.");
            }

            // Only the named, audited operation shapes are admitted. Unknown Roslyn kinds fail closed.
            if (!_admittedOperations.Contains(operation.Kind))
                return Reject("STAGE-ESM-022", $"Operation '{operation.Kind}' on '{operation.Type?.ToDisplayString()}' is outside the pure allowlist.");

            var wrapper = $"TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}";
            if ((operation is IParameterReferenceOperation or ILocalReferenceOperation &&
                operation.Type?.Name == wrapper &&
                operation.Parent is not IPropertyReferenceOperation { Instance: { } instance }) ||
                (operation is IParameterReferenceOperation or ILocalReferenceOperation &&
                operation.Type?.Name == wrapper && operation.Parent is IPropertyReferenceOperation propertyInstance &&
                propertyInstance.Instance != operation))
            {
                return Reject("STAGE-ESM-022", $"Wrapper '{wrapper}' may only be used through a property getter.");
            }

            if ((operation is IIsPatternOperation { Value.Type.Name: var patternType } && patternType == wrapper) ||
                (operation is IWithOperation { Operand.Type.Name: var withType } && withType == wrapper) ||
                (operation is IDeconstructionAssignmentOperation { Value.Type.Name: var deconstructedType } && deconstructedType == wrapper) ||
                (operation is IArgumentOperation { Value.Type.Name: var argumentType } && argumentType == wrapper))
            {
                return Reject("STAGE-ESM-022", $"Wrapper '{wrapper}' may only be used through a property getter.");
            }

            if (operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.Add, Type.SpecialType: SpecialType.System_String } concatenation &&
                (!StringOrChar(concatenation.LeftOperand) || !StringOrChar(concatenation.RightOperand)))
            {
                return Reject("STAGE-ESM-022", "Culture-sensitive string.Concat operands are not pure.");
            }

            if (operation is IInvocationOperation concat && concat.TargetMethod.Name == "Concat" &&
                concat.TargetMethod.ContainingType.SpecialType == SpecialType.System_String &&
                concat.Arguments.Any(argument => !StringOrChar(argument.Value)))
            {
                return Reject("STAGE-ESM-022", $"Symbol '{concat.TargetMethod.ToDisplayString()}' uses culture-sensitive object conversion.");
            }

            if (operation is IInvocationOperation invocation && invocation.TargetMethod.Parameters.Any(_ => _.Type.Name == "IFormatProvider") &&
                invocation.Arguments.Where(_ => _.Parameter?.Type.Name == "IFormatProvider").Any(argument =>
                    !IsInvariantProvider(argument.Value)))
            {
                return Reject("STAGE-ESM-022", $"Symbol '{invocation.TargetMethod.ToDisplayString()}' requires CultureInfo.InvariantCulture.");
            }

            var symbol = operation switch
            {
                IInvocationOperation call => call.TargetMethod,
                IObjectCreationOperation creation => creation.Constructor,
                IPropertyReferenceOperation property => property.Property,
                IFieldReferenceOperation field => field.Field,
                IMethodReferenceOperation methodReference => methodReference.Method,
                IConversionOperation { OperatorMethod: { } conversion } => conversion,
                IBinaryOperation { OperatorMethod: { } binary } => binary,
                IUnaryOperation { OperatorMethod: { } unary } => unary,
                IForEachLoopOperation loop when loop.Syntax is CommonForEachStatementSyntax syntax =>
                    model.GetForEachStatementInfo(syntax).GetEnumeratorMethod,
                IDeconstructionAssignmentOperation { Syntax: AssignmentExpressionSyntax deconstruction } =>
                    model.GetDeconstructionInfo(deconstruction).Method,
                IRecursivePatternOperation recursive => recursive.DeconstructSymbol,
                _ => null
            };
            if ((operation is IDeconstructionAssignmentOperation && symbol is null) ||
                (operation is IForEachLoopOperation && symbol is null) ||
                (operation is IRecursivePatternOperation { DeconstructionSubpatterns.Length: > 0 } && symbol is null))
            {
                return Reject("STAGE-ESM-022", $"Unresolved operation '{operation.Kind}' is outside the pure allowlist.");
            }

            if (symbol is null) continue;
            if (operation is IPropertyReferenceOperation propertyRead && propertyRead.Property.ContainingType.Name == wrapper)
            {
                if (propertyRead.Property.Name == "Tenant")
                    return Reject("STAGE-ESM-022", $"Reading symbol '{propertyRead.Property.ToDisplayString()}' is not admitted until the default tenant is defined.");
                if ((propertyRead.Parent is ISimpleAssignmentOperation { Target: var target } && target == propertyRead) ||
                    propertyRead.Parent is ICompoundAssignmentOperation or IIncrementOrDecrementOperation or ICoalesceAssignmentOperation)
                {
                    return Reject("STAGE-ESM-022", $"Writing wrapper property '{propertyRead.Property.ToDisplayString()}' is not pure.");
                }

                reads.Add(propertyRead.Property.Name);
            }

            if (!Allowed(symbol, model.Compilation, model.GetDeclaredSymbol((MethodDeclarationSyntax)body.Parent!)!.Parameters[0].Type.ContainingNamespace.ContainingNamespace.ToDisplayString(), out var entry) ||
                (symbol.ContainingType?.Name == wrapper && operation is not IPropertyReferenceOperation))
            {
                return Reject("STAGE-ESM-022", $"Symbol '{symbol.ToDisplayString()}' is outside the pure allowlist.");
            }

            used.Add(entry);
        }

        return new(null, null, reads.ToImmutable(), used.ToImmutable());
    }

    static bool IsInvariantProvider(IOperation operation)
    {
        while (operation is IConversionOperation { OperatorMethod: null } conversion)
        {
            operation = conversion.Operand;
        }

        return operation is IPropertyReferenceOperation property && property.Property.Name == "InvariantCulture" &&
            property.Property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Globalization.CultureInfo";
    }

    static bool StringOrChar(IOperation operation)
    {
        while (operation is IConversionOperation conversion) operation = conversion.Operand;
        return operation.Type?.SpecialType is SpecialType.System_String or SpecialType.System_Char;
    }

    static bool Allowed(ISymbol symbol, Compilation compilation, string rootNamespace, out string entry)
    {
        entry = string.Empty;
        if (symbol is IMethodSymbol { MethodKind: MethodKind.BuiltinOperator })
        {
            entry = "primitive";
            return true;
        }

        var type = symbol.ContainingType;
        if (type is null)
        {
            return false;
        }

        var full = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var name = symbol.Name;
        if (name == "GetHashCode") return false;
        if (type.IsAnonymousType) return false; // Anonymous records can carry randomized string hashes; no synthesized method is trusted.
        if (symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction } local)
        {
            entry = "generated";
            return local.Locations.All(_ => _.IsInSource && compilation.SyntaxTrees.Contains(_.SourceTree)) &&
                local.DeclaringSyntaxReferences.Any(_ => _.GetSyntax() is LocalFunctionStatementSyntax { Body: not null } or
                    LocalFunctionStatementSyntax { ExpressionBody: not null });
        }

        // Both the declaring type and the member must belong to this exact synthetic source tree.
        // This excludes Microsoft.Win32, all referenced application assemblies and unrelated source files.
        if (type.ContainingAssembly.Name == compilation.AssemblyName &&
            type.Locations.Length > 0 && type.Locations.All(_ => _.IsInSource && compilation.SyntaxTrees.Contains(_.SourceTree)) &&
            (type.ContainingNamespace.ToDisplayString() == rootNamespace ||
             type.ContainingNamespace.ToDisplayString().StartsWith(rootNamespace + ".", StringComparison.Ordinal)) &&
            symbol.Locations.All(_ => _.IsInSource && compilation.SyntaxTrees.Contains(_.SourceTree)))
        {
            entry = "generated";
            return name != "ToString" && (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.PropertyGet or MethodKind.UserDefinedOperator } or IPropertySymbol or IFieldSymbol ||
                (symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary } method &&
                    method.DeclaringSyntaxReferences.Any(_ => compilation.SyntaxTrees.Contains(_.SyntaxTree))));
        }

        if (type.SpecialType is >= SpecialType.System_Boolean and <= SpecialType.System_String)
        {
            if (full == "global::System.String")
            {
                entry = "string";
                if (symbol is IMethodSymbol stringMethod && NameIs(stringMethod.Name, "ToString", "Format", "Join"))
                {
                    return false;
                }

                if (symbol is IMethodSymbol compare && NameIs(compare.Name, "Compare", "Equals", "Contains", "StartsWith", "EndsWith", "IndexOf") &&
                    compare.Parameters.Length > 0 && !compare.Parameters.Any(parameter => NameIs(parameter.Type.Name, "StringComparison", "IFormatProvider")))
                {
                    return false;
                }

                if (name == "Concat" && symbol is IMethodSymbol concat &&
                    concat.Parameters.Any(_ => _.Type.SpecialType != SpecialType.System_String))
                {
                    return false;
                }

                return NameIs(name, "Length", "Empty", "Chars", "Substring", "IsNullOrEmpty", "IsNullOrWhiteSpace", "Concat", "Replace", "Trim", "Equals", "Compare", "Contains", "StartsWith", "EndsWith", "IndexOf", "op_Equality", "op_Inequality", ".ctor");
            }

            entry = "primitive";
            return name != "GetHashCode" && (!NameIs(name, "Parse", "TryParse", "ToString") ||
                (symbol is IMethodSymbol { Parameters.Length: > 0 } method && method.Parameters.Any(parameter => parameter.Type.Name == "IFormatProvider"))) &&
                !(type.SpecialType == SpecialType.System_Char && NameIs(name, "ToUpper", "ToLower"));
        }

        if (full.StartsWith("global::System.Nullable<", StringComparison.Ordinal) || type.IsTupleType)
        {
            entry = "primitive";
            return NameIs(name, "HasValue", "Value", "GetValueOrDefault", ".ctor", "Item1", "Item2", "Item3", "Item4");
        }

        if (NameIs(full, "global::System.Math", "global::System.MathF"))
        {
            entry = "math";
            return NameIs(name, "Abs", "Min", "Max", "Clamp", "Round", "Floor", "Ceiling", "Truncate", "Pow", "Sqrt");
        }

        if (NameIs(full, "global::System.DateTimeOffset", "global::System.DateTime", "global::System.DateOnly", "global::System.TimeSpan"))
        {
            entry = "time";
            return !NameIs(name, "Now", "UtcNow", "Today", "Parse", "TryParse", "ToString", "ToLocalTime", "LocalDateTime", "ToShortDateString", "ToLongDateString", "ToShortTimeString", "ToLongTimeString");
        }

        if (full == "global::System.StringComparison")
        {
            entry = "string";
            return symbol is IFieldSymbol && NameIs(name, "Ordinal", "OrdinalIgnoreCase", "InvariantCulture", "InvariantCultureIgnoreCase");
        }

        if (full == "global::System.Guid")
        {
            entry = "guid";
            return !NameIs(name, "NewGuid", "CreateVersion7", "Parse", "TryParse", "ToString");
        }

        if (full == "global::System.Globalization.CultureInfo")
        {
            entry = "string";
            return symbol is IPropertySymbol && name == "InvariantCulture";
        }

        if (full == "global::System.StringComparer")
        {
            entry = "string";
            return symbol is IPropertySymbol && NameIs(name, "Ordinal", "OrdinalIgnoreCase");
        }

        if (full.StartsWith("global::System.Collections.", StringComparison.Ordinal))
        {
            entry = "collections";

            // Refuse hash collections (including immutable hash types) altogether, not just enumeration:
            // a later operation can silently start depending on their process-randomized ordering.
            return (full.StartsWith("global::System.Collections.Generic.List<", StringComparison.Ordinal) ||
                    full.StartsWith("global::System.Collections.Generic.IReadOnlyList<", StringComparison.Ordinal) ||
                    full.StartsWith("global::System.Collections.Generic.IEnumerable<", StringComparison.Ordinal) ||
                    full.StartsWith("global::System.Collections.Immutable.ImmutableArray<", StringComparison.Ordinal)) &&
                NameIs(name, ".ctor", "Add", "Count", "Length", "Item", "GetEnumerator", "Current", "MoveNext", "Dispose", "ToArray", "Empty");
        }

        if (full == "global::System.Linq.Enumerable")
        {
            entry = "linq";
            return NameIs(name, "Where", "Select", "SelectMany", "Any", "All", "Count", "LongCount", "First", "FirstOrDefault", "Last", "LastOrDefault", "Single", "SingleOrDefault", "Take", "Skip", "Sum", "Aggregate", "ToArray", "ToList");
        }

        if (ExceptionType(type))
        {
            entry = "exceptions";
            return symbol is IMethodSymbol { MethodKind: MethodKind.Constructor };
        }

        return false;
    }

    static bool ExceptionType(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Exception")
            {
                return true;
            }
        }

        return false;
    }

    static bool NameIs(string name, params string[] names) => names.Contains(name, StringComparer.Ordinal);

    static IEnumerable<IOperation> Descendants(IOperation root)
    {
        yield return root;
        foreach (var child in root.ChildOperations)
            foreach (var nested in Descendants(child)) yield return nested;
    }

    static string SourceLocation(SemanticImplementationRequirement requirement, string body, int offset)
    {
        var line = body.AsSpan(0, offset).Count('\n');
        var start = body.LastIndexOf('\n', Math.Max(0, offset - 1));
        var column = offset - start;
        if (requirement.File is not null) return $"{requirement.File}:{line + 1}:{column}";
        if (!requirement.BodyLines.IsDefaultOrEmpty && line < requirement.BodyLines.Length)
            return $"{requirement.Source.Span.Document}:{requirement.BodyLines[line].Line}:{requirement.BodyLines[line].Column + column - 1}";
        return $"line {requirement.BodySpan?.StartLine + line}, column {column}";
    }

    static Verdict Reject(string code, string reason) => new(code, reason, [], []);

    internal sealed record Verdict(string? Code, string? Reason, ImmutableHashSet<string> ContextReads,
        ImmutableHashSet<string> UsedAllowlistEntries)
    {
        internal bool Accepted => Code is null;
    }
}
