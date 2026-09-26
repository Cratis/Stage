// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
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
        ["generated"] = "Stage-owned data records, wrappers and concept value members; remove if generated types gain executable user code.",
        ["primitive"] = "CLR primitive arithmetic and immutable values; remove if a primitive member consults ambient state.",
        ["math"] = "Deterministic numeric operations; remove if Math grows stateful operations.",
        ["time"] = "Value arithmetic, never Now/UtcNow/Today; remove if a member consults ambient time.",
        ["guid"] = "Guid value operations, never NewGuid/CreateVersion7; remove if a member consults randomness.",
        ["string"] = "Ordinal and provider-explicit text operations only; remove if culture-independent behaviour changes.",
        ["collections"] = "In-memory collection construction/traversal; remove if APIs introduce external effects.",
        ["linq"] = "In-memory Enumerable operators only; remove if methods gain external effects.",
        ["exceptions"] = "System exception construction for explicit throw; remove if constructors acquire side effects."
    };

    // BCL references are explicit: no Arc, Chronicle, Screenplay or application assembly enters this compilation.
    internal static readonly Lazy<MetadataReference[]> _references = new(() =>
    {
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "System.Private.CoreLib.dll", "System.Runtime.dll", "System.Linq.dll", "System.Collections.dll",
            "System.Collections.Immutable.dll"
        };
        return [.. AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location) && names.Contains(Path.GetFileName(assembly.Location)))
            .Concat([typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(ImmutableArray).Assembly,
                Assembly.Load("System.Runtime"), Assembly.Load("System.Collections")])
            .DistinctBy(assembly => assembly.Location)
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))];
    });

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
                AnonymousFunctionExpressionSyntax { AsyncKeyword.RawKind: not 0 } ||
            (node is LocalFunctionStatementSyntax local && local.Modifiers.Any(SyntaxKind.AsyncKeyword)));
        if (forbidden is not null)
        {
            return Reject("STAGE-ESM-022", $"Forbidden pure construct '{forbidden.Kind()}'.");
        }

        var ns = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(readModel.Id).Path);
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

        definitions.Add($"namespace {ns} {{ public record {Identifiers.ToPascalCase(readModel.Name)}({Parameters(readModel.Properties)}); }}");
        definitions.Add($"namespace {eventNs} {{ public record {Identifiers.ToPascalCase(@event.Name)}({Parameters(@event.Properties)}); }}");
        var wrapper = SemanticTypedContextRenderer.Render(descriptor, context).Content
            .Replace($"namespace {context.RootNamespace}.TypedContexts;", $"namespace {context.RootNamespace}.TypedContexts {{", StringComparison.Ordinal) + "\n}";
        var modelType = $"global::{ns}.{Identifiers.ToPascalCase(readModel.Name)}";
        var wrapperType = $"global::{context.RootNamespace}.TypedContexts.TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}";
        var tenant = $"namespace {context.RootNamespace}.TypedContexts {{ public record TenantId(string Value); }}";
        var prefix = "using System; using System.Collections.Generic; using System.Collections.Immutable; using System.Linq; " +
            (context.Application.Concepts.IsEmpty && context.Application.Types.IsEmpty ? string.Empty : $"using {context.RootNamespace}.Common; ");
        var code = prefix + string.Join('\n', definitions) + "\n" + tenant + "\n" + wrapper + "\n" +
            $"namespace {ns} {{ public static class TransitionAnalysis {{ public static {modelType}? Transition({wrapperType} context) {{\n" + body + "\n} } }";
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "PureTransitionAnalysis",
            [tree],
            _references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            var error = errors[0];
            var methodOffset = code.IndexOf(body, code.LastIndexOf("public static class TransitionAnalysis", StringComparison.Ordinal), StringComparison.Ordinal);
            var offset = Math.Clamp(error.Location.SourceSpan.Start - methodOffset, 0, body.Length);
            var location = SourceLocation(requirement, body, offset);
            return Reject("STAGE-ESM-019", $"Reducer body {location}: {error.Id}: {error.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}");
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

            ISymbol? symbol = operation switch
            {
                IInvocationOperation call => call.TargetMethod,
                IObjectCreationOperation creation => creation.Constructor,
                IPropertyReferenceOperation property => property.Property,
                IFieldReferenceOperation field => field.Field,
                IMethodReferenceOperation methodReference => methodReference.Method,
                IConversionOperation { OperatorMethod: { } conversion } => conversion,
                IBinaryOperation { OperatorMethod: { } binary } => binary,
                IUnaryOperation { OperatorMethod: { } unary } => unary,
                _ => null
            };
            if (symbol is null) continue;
            if (operation is IPropertyReferenceOperation propertyRead &&
                propertyRead.Property.ContainingType.Name == $"TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}")
            {
                if (propertyRead.Property.Name == "Tenant")
                {
                    return Reject("STAGE-ESM-022", $"Reading symbol '{propertyRead.Property.ToDisplayString()}' is not admitted until the default tenant is defined.");
                }

                reads.Add(propertyRead.Property.Name);
            }

            if (!Allowed(symbol, out var entry))
            {
                return Reject("STAGE-ESM-022", $"Symbol '{symbol.ToDisplayString()}' is outside the pure allowlist.");
            }
            used.Add(entry);
        }

        return new(null, null, reads.ToImmutable(), used.ToImmutable());
    }

    static bool Allowed(ISymbol symbol, out string entry)
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
        if (!type.ContainingNamespace.ToDisplayString().StartsWith("System", StringComparison.Ordinal))
        {
            entry = "generated";
            return name != "ToString" &&
                (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.PropertyGet or MethodKind.UserDefinedOperator or MethodKind.BuiltinOperator } or IPropertySymbol or IFieldSymbol ||
                (symbol is IMethodSymbol && NameIs(name, "Deconstruct", "Equals", "GetHashCode")));
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

                return NameIs(name, "Length", "Empty", "Chars", "Substring", "IsNullOrEmpty", "IsNullOrWhiteSpace", "Concat", "Replace", "Trim", "Equals", "Compare", "Contains", "StartsWith", "EndsWith", "IndexOf", "op_Equality", "op_Inequality", ".ctor");
            }

            entry = "primitive";
            return !NameIs(name, "Parse", "TryParse", "ToString") ||
                (symbol is IMethodSymbol { Parameters.Length: > 0 } method && method.Parameters.Any(parameter => parameter.Type.Name == "IFormatProvider"));
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
            return !NameIs(name, "Now", "UtcNow", "Today", "Parse", "TryParse", "ToString");
        }

        if (full == "global::System.StringComparison")
        {
            entry = "string";
            return symbol is IFieldSymbol;
        }

        if (full == "global::System.Guid")
        {
            entry = "guid";
            return !NameIs(name, "NewGuid", "CreateVersion7", "Parse", "TryParse", "ToString");
        }

        if (full.StartsWith("global::System.Collections.", StringComparison.Ordinal))
        {
            entry = "collections";
            return name != "ToString";
        }

        if (full == "global::System.Linq.Enumerable")
        {
            entry = "linq";
            return name != "ToString";
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
