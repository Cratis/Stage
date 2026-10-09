// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Closed-compilation, fail-closed symbol admission for attached pure transitions (not a sandbox).</summary>
internal static class PureTransitionAdmission
{
    // Exact net10.0 reference pack audited for reducer binding. Never silently substitute a newer pack.
    internal const string ReferencePackVersion = "10.0.12";

    // Exact BCL signatures, not broad type/name categories. The parity fixtures must cover each entry.
    internal static readonly IReadOnlySet<string> AuditedMemberSignatures = new HashSet<string>(StringComparer.Ordinal)
    {
        "Field:global::System.Collections.Immutable.ImmutableArray<T>.Empty:global::System.Collections.Immutable.ImmutableArray<T>",
        "Field:global::System.MidpointRounding.ToEven:global::System.MidpointRounding",
        "Field:global::System.StringComparison.Ordinal:global::System.StringComparison",
        "Method:decimal.ToString`0(None:global::System.IFormatProvider):string",
        "Method:global::System.ArgumentException..ctor`0(None:string):void",
        "Method:global::System.Collections.Immutable.ImmutableArray.ToImmutableArray`1(None:global::System.Collections.Generic.IEnumerable<TSource>):global::System.Collections.Immutable.ImmutableArray<TSource>",
        "Method:global::System.Collections.Immutable.ImmutableArray<T>.Enumerator.MoveNext`0():bool",
        "Method:global::System.Collections.Immutable.ImmutableArray<T>.GetEnumerator`0():global::System.Collections.Immutable.ImmutableArray<T>.Enumerator",
        "Method:global::System.DateTimeOffset.AddTicks`0(None:long):global::System.DateTimeOffset",
        "Method:global::System.DateTimeOffset.op_Addition`0(None:global::System.DateTimeOffset,None:global::System.TimeSpan):global::System.DateTimeOffset",
        "Method:global::System.DateTimeOffset.op_GreaterThanOrEqual`0(None:global::System.DateTimeOffset,None:global::System.DateTimeOffset):bool",
        "Method:global::System.DateTimeOffset.op_Subtraction`0(None:global::System.DateTimeOffset,None:global::System.DateTimeOffset):global::System.TimeSpan",
        "Method:global::System.InvalidOperationException..ctor`0(None:string):void",
        "Method:global::System.Linq.Enumerable.Aggregate`2(None:global::System.Collections.Generic.IEnumerable<TSource>,None:TAccumulate,None:global::System.Func<TAccumulate, TSource, TAccumulate>):TAccumulate",
        "Method:global::System.Linq.Enumerable.All`1(None:global::System.Collections.Generic.IEnumerable<TSource>,None:global::System.Func<TSource, bool>):bool",
        "Method:global::System.Linq.Enumerable.Any`1(None:global::System.Collections.Generic.IEnumerable<TSource>,None:global::System.Func<TSource, bool>):bool",
        "Method:global::System.Linq.Enumerable.Concat`1(None:global::System.Collections.Generic.IEnumerable<TSource>,None:global::System.Collections.Generic.IEnumerable<TSource>):global::System.Collections.Generic.IEnumerable<TSource>",
        "Method:global::System.Linq.Enumerable.Count`1(None:global::System.Collections.Generic.IEnumerable<TSource>,None:global::System.Func<TSource, bool>):int",
        "Method:global::System.Linq.Enumerable.FirstOrDefault`1(None:global::System.Collections.Generic.IEnumerable<TSource>):TSource",
        "Method:global::System.Linq.Enumerable.First`1(None:global::System.Collections.Generic.IEnumerable<TSource>):TSource",
        "Method:global::System.Linq.Enumerable.LastOrDefault`1(None:global::System.Collections.Generic.IEnumerable<TSource>):TSource",
        "Method:global::System.Linq.Enumerable.Last`1(None:global::System.Collections.Generic.IEnumerable<TSource>):TSource",
        "Method:global::System.Linq.Enumerable.Select`2(None:global::System.Collections.Generic.IEnumerable<TSource>,None:global::System.Func<TSource, TResult>):global::System.Collections.Generic.IEnumerable<TResult>",
        "Method:global::System.Linq.Enumerable.Skip`1(None:global::System.Collections.Generic.IEnumerable<TSource>,None:int):global::System.Collections.Generic.IEnumerable<TSource>",
        "Method:global::System.Linq.Enumerable.Sum`0(None:global::System.Collections.Generic.IEnumerable<decimal>):decimal",
        "Method:global::System.Linq.Enumerable.Take`1(None:global::System.Collections.Generic.IEnumerable<TSource>,None:int):global::System.Collections.Generic.IEnumerable<TSource>",
        "Method:global::System.Linq.Enumerable.ToArray`1(None:global::System.Collections.Generic.IEnumerable<TSource>):TSource[]",
        "Method:global::System.Linq.Enumerable.Where`1(None:global::System.Collections.Generic.IEnumerable<TSource>,None:global::System.Func<TSource, bool>):global::System.Collections.Generic.IEnumerable<TSource>",
        "Method:global::System.Math.Abs`0(None:decimal):decimal",
        "Method:global::System.Math.Abs`0(None:int):int",
        "Method:global::System.Math.Ceiling`0(None:decimal):decimal",
        "Method:global::System.Math.Clamp`0(None:decimal,None:decimal,None:decimal):decimal",
        "Method:global::System.Math.Floor`0(None:decimal):decimal",
        "Method:global::System.Math.Max`0(None:decimal,None:decimal):decimal",
        "Method:global::System.Math.Min`0(None:decimal,None:decimal):decimal",
        "Method:global::System.Math.Round`0(None:decimal,None:global::System.MidpointRounding):decimal",
        "Method:global::System.Math.Sign`0(None:decimal):int",
        "Method:global::System.Math.Truncate`0(None:decimal):decimal",
        "Method:global::System.TimeSpan.Add`0(None:global::System.TimeSpan):global::System.TimeSpan",
        "Method:global::System.TimeSpan.op_Equality`0(None:global::System.TimeSpan,None:global::System.TimeSpan):bool",
        "Method:string.Contains`0(None:string,None:global::System.StringComparison):bool",
        "Method:string.EndsWith`0(None:string,None:global::System.StringComparison):bool",
        "Method:string.Equals`0(None:string,None:string,None:global::System.StringComparison):bool",
        "Method:string.IndexOf`0(None:string,None:global::System.StringComparison):int",
        "Method:string.IsNullOrEmpty`0(None:string):bool",
        "Method:string.StartsWith`0(None:string,None:global::System.StringComparison):bool",
        "Method:string.Substring`0(None:int,None:int):string",
        "Property:global::System.Collections.Immutable.ImmutableArray<T>.Enumerator.Current():T",
        "Property:global::System.Collections.Immutable.ImmutableArray<T>.Length():int",
        "Property:global::System.Collections.Immutable.ImmutableArray<T>.this[](int):T",
        "Property:global::System.DateTimeOffset.Day():int",
        "Property:global::System.DateTimeOffset.Hour():int",
        "Property:global::System.DateTimeOffset.Minute():int",
        "Property:global::System.DateTimeOffset.Month():int",
        "Property:global::System.DateTimeOffset.Second():int",
        "Property:global::System.DateTimeOffset.Ticks():long",
        "Property:global::System.DateTimeOffset.Year():int",
        "Property:global::System.Globalization.CultureInfo.InvariantCulture():global::System.Globalization.CultureInfo",
        "Property:global::System.TimeSpan.Ticks():long",
        "Property:string.Length():int",
        "Property:string.this[](int):char"
    };

    // Each entry is a separate exemption; a fixture must exercise it and any new side effect ends the exemption.
    internal static readonly IReadOnlyDictionary<string, string> AllowlistReasons = new Dictionary<string, string>
    {
        ["generated"] = "Source-declared Stage-owned records, wrappers and concept value members; no anonymous types or generated hash methods.",
        ["primitive"] = "Fixed-width integer and decimal arithmetic, bool and char values, and nullable value access.",
        ["math"] = "Audited integer and decimal Math members only.",
        ["time"] = "Explicit DateTimeOffset and TimeSpan arithmetic and component reads only.",
        ["string"] = "Ordinal text operations and invariant numeric formatting only.",
        ["collections"] = "ImmutableArray and local array reads; no mutable or hash collection members.",
        ["linq"] = "Audited order-preserving LINQ with expression-only callbacks and no deferred composition.",
        ["exceptions"] = "Only explicit InvalidOperationException(string) and ArgumentException(string) throws."
    };

    // These are the 10.0.12 package's assemblies embedded in the renderer, independent of
    // the installed SDK or host runtime. Never fall back to another installed SDK pack.
    internal static readonly Lazy<MetadataReference[]> _references = new(LoadEmbeddedReferences);

    // Isolated override for failure-path specifications; it does not alter the cached production pack.
    internal static readonly AsyncLocal<string?> ReferenceDirectoryOverride = new();

    static readonly HashSet<OperationKind> _admittedOperations =
    [
        OperationKind.Block, OperationKind.Return, OperationKind.Throw, OperationKind.ExpressionStatement,
        OperationKind.VariableDeclarationGroup, OperationKind.VariableDeclaration, OperationKind.VariableDeclarator,
        OperationKind.VariableInitializer, OperationKind.LocalReference, OperationKind.ParameterReference,
        OperationKind.Literal, OperationKind.DefaultValue,
        OperationKind.Invocation, OperationKind.ObjectCreation, OperationKind.AnonymousFunction, OperationKind.DelegateCreation,
        OperationKind.Argument, OperationKind.Conversion, OperationKind.Binary,
        OperationKind.Unary, OperationKind.Conditional, OperationKind.Coalesce, OperationKind.ConditionalAccess,
        OperationKind.ConditionalAccessInstance, OperationKind.PropertyReference, OperationKind.FieldReference,
        OperationKind.InstanceReference, OperationKind.SimpleAssignment,
        OperationKind.CompoundAssignment, OperationKind.Increment, OperationKind.Decrement,
        OperationKind.ArrayCreation, OperationKind.ArrayInitializer, OperationKind.ArrayElementReference,
        OperationKind.ObjectOrCollectionInitializer,
        OperationKind.IsPattern, OperationKind.DeclarationPattern, OperationKind.ConstantPattern,
        OperationKind.DiscardPattern, OperationKind.NegatedPattern, OperationKind.Loop,
        OperationKind.Branch, OperationKind.Empty, OperationKind.Switch, OperationKind.SwitchCase,
        OperationKind.CaseClause, OperationKind.SwitchExpression, OperationKind.SwitchExpressionArm,
        OperationKind.Discard, OperationKind.With
    ];

    internal static Verdict Analyze(
        string body,
        SemanticApplicationContext context,
        SemanticReadModel readModel,
        SemanticEventContract @event,
        SemanticTypedContextDescriptor descriptor,
        SemanticImplementationRequirement requirement)
    {
        // Reject disallowed type tests before diagnostics: an ImmutableArray input cannot legally
        // bind to a mutable List or array, but that is a purity refusal, not an ordinary compile error.
        var generatedNames = context.Application.Concepts.Select(_ => Identifiers.ToPascalCase(_.Name))
            .Concat(context.Application.Types.Select(_ => Identifiers.ToPascalCase(_.Name)))
            .Append(Identifiers.ToPascalCase(readModel.Name))
            .Append(Identifiers.ToPascalCase(@event.Name))
            .ToHashSet(StringComparer.Ordinal);
        var parsed = ParseBody(body, generatedNames, "reducer");
        if (parsed.Rejection is not null) return parsed.Rejection;

        var modelNs = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(readModel.Id).Path);

        // A specification may refer to a reducer outside the requested output scope.
        var located = context.DeclaringSlice(readModel.Id);
        var reducers = located.Slice.Reducers.Where(candidate => candidate.ReadModel == readModel.Id).ToArray();
        if (reducers.Length != 1)
            return Reject("STAGE-ESM-021", $"Read model '{readModel.Name}' has no unique reducer to analyse.");
        var reducer = reducers[0];
        var reducerName = Identifiers.ToPascalCase(reducer.Name);

        // Synthetic stubs contain only the current model and observed events. Inventory every
        // declaration imported by the real file, including constraint classes in event slices.
        // Exclude only the exact stubbed declaration, never an unrelated peer with the same short name.
        var reducerEvents = reducer.Transitions.Select(transition => context.Events[transition.EventContract]).ToArray();
        var importedSlices = reducerEvents.Select(@event => context.DeclaringSlice(@event.Id).Slice).Append(located.Slice).Distinct();
        var stubbed = reducerEvents.Select(@event => (
                Id: (SemanticId?)@event.Id,
                Namespace: SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path),
                Name: Identifiers.ToPascalCase(@event.Name)))
            .Append(((SemanticId?)readModel.Id, modelNs, Identifiers.ToPascalCase(readModel.Name)))
            .ToHashSet();
        var peerDeclarations = importedSlices.SelectMany(slice =>
        {
            var sliceNamespace = SliceNaming.Namespace(context.RootNamespace, context.Slice(slice.Id).Path);
            return slice.ReadModels.Select(_ => (Id: (SemanticId?)_.Id, _.Name))
                .Concat(slice.Events.Select(_ => (Id: (SemanticId?)_.Id, _.Name)))
                .Concat(slice.Commands.Select(_ => (Id: (SemanticId?)_.Id, _.Name)))
                .Concat(slice.Reducers.Select(_ => (Id: (SemanticId?)null, _.Name)))
                .Concat(slice.Projections.Where(_ => _.Scope is not null).Select(_ => (Id: (SemanticId?)_.Id, _.Name)))
                .Concat(slice.Constraints.Select(_ => (Id: (SemanticId?)null, _.Name)))
                .Concat(slice.Commands.Select(_ => (Id: (SemanticId?)null, Name: _.Name + "Validator")))
                .Select(_ => (_.Id, Namespace: sliceNamespace, Name: Identifiers.ToPascalCase(_.Name)));
        }).Concat(context.Application.Concepts.Select(_ => (Id: (SemanticId?)_.Id, Namespace: context.CommonNamespace, Name: Identifiers.ToPascalCase(_.Name))))
            .Concat(context.Application.Types.Select(_ => (Id: (SemanticId?)_.Id, Namespace: context.CommonNamespace, Name: Identifiers.ToPascalCase(_.Name))));
        var allDeclarations = peerDeclarations.ToArray();
        var peers = allDeclarations.Where(declaration => !stubbed.Contains(declaration))
            .Select(declaration => declaration.Name).ToHashSet(StringComparer.Ordinal);

        // Emitted property types are qualified (global::{Root}.Common.X or global::System.*) in the
        // rendered reducer, its typed-context wrapper and the synthetic analysis stubs, so a same-named
        // sibling declaration or namespace cannot rebind them. Unqualified names in the reducer body are
        // checked separately below.
        var activeShadow = reducerEvents.Select(_ => Identifiers.ToPascalCase(_.Name))
            .Append(Identifiers.ToPascalCase(readModel.Name)).FirstOrDefault(ShadowsAuditedName);
        if (activeShadow is not null)
            return Reject("STAGE-ESM-022", $"Generated type '{activeShadow}' shadows an audited type in the reducer namespace.");
        var namespacePaths = context.NamespacePaths.GroupBy(path => path.Split('.')[^1], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToHashSet(StringComparer.Ordinal), StringComparer.Ordinal);
        var ns = SliceNaming.Namespace(context.RootNamespace, located.Path);
        var types = new SemanticTypeSystem(context);
        var definitions = new List<string>();
        foreach (var concept in context.Application.Concepts)
        {
            if (!concept.Values.IsEmpty)
            {
                definitions.Add($"namespace {context.CommonNamespace} {{ public enum {Identifiers.ToPascalCase(concept.Name)} {{ {string.Join(", ", concept.Values.Select(Identifiers.ToPascalCase))} }} }}");
                continue;
            }

            var scalar = SemanticTypeSystem.Primitive(concept.Primitive);

            // Mirror the inherited concept getters, including the distinct TypedValue on identifiers.
            // Synthetic concepts intentionally have no implicit conversions: extra refusals are safe.
            var baseType = context.IdentifierConcepts.Contains(concept.Id)
                ? $"global::Cratis.Chronicle.Events.EventSourceId<{scalar}>"
                : $"global::Cratis.Concepts.ConceptAs<{scalar}>";
            definitions.Add($"namespace {context.CommonNamespace} {{ public record {Identifiers.ToPascalCase(concept.Name)}({scalar} Value) : {baseType}(Value); }}");
        }

        foreach (var composite in context.Application.Types)
        {
            definitions.Add($"namespace {context.CommonNamespace} {{ public record {Identifiers.ToPascalCase(composite.Name)}({Parameters(composite.Properties)}); }}");
        }

        definitions.Add($"namespace {modelNs} {{ public record {Identifiers.ToPascalCase(readModel.Name)}({Parameters(SemanticStateViewArtifactRenderer.OrderedProperties(readModel.Properties), true)}); }}");
        foreach (var transitionEvent in reducerEvents)
        {
            var transitionNs = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(transitionEvent.Id).Path);
            definitions.Add($"namespace {transitionNs} {{ public record {Identifiers.ToPascalCase(transitionEvent.Name)}({Parameters(transitionEvent.Properties, true)}); }}");
        }
        var wrapper = SemanticTypedContextRenderer.Render(descriptor, context).Content
            .Replace($"namespace {context.RootNamespace}.TypedContexts;", $"namespace {context.RootNamespace}.TypedContexts {{", StringComparison.Ordinal) + "\n}";
        var modelType = $"global::{modelNs}.{Identifiers.ToPascalCase(readModel.Name)}";
        var wrapperType = $"global::{context.RootNamespace}.TypedContexts.TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}";
        var tenant = $"namespace {context.RootNamespace}.TypedContexts {{ public record TenantId(string Value) {{ public static readonly TenantId Default = new(\"00000000-0000-0000-0000-000000000000\"); public static readonly TenantId NotSet = new(\"\"); }} }}";

        // Model stubs are compilation peers, not imports into the reducer file. The body has the
        // same namespace and exact using directives as SemanticReducerArtifactRenderer.Render.
        var declarations = "global using System; global using System.Collections.Generic; global using System.Linq; " +
            "global using System.IO; global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks; " +
            (context.Application.Concepts.IsEmpty && context.Application.Types.IsEmpty ? string.Empty : $"using {context.CommonNamespace}; ") +
            "namespace Cratis.Concepts { public record ConceptAs<T>(T Value); } " +
            "namespace Cratis.Chronicle.Events { public class EventContext {} public record EventSourceId<T>(T TypedValue) : global::Cratis.Concepts.ConceptAs<T>(TypedValue); } " +
            "namespace Cratis.Chronicle.Reducers { public interface IReducerFor<T> {} } " +
            string.Join('\n', definitions) + "\n" + tenant + "\n" + wrapper;
        var builder = new CodeGeneration.CSharpCodeBuilder()
            .Namespace(ns)
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Reducers")
            .Using($"{context.RootNamespace}.TypedContexts")
            .Using(modelNs);
        foreach (var transitionEvent in reducerEvents)
        {
            builder.Using(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(transitionEvent.Id).Path));
        }

        var suffix = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(descriptor.RequirementId)))[..8];
        builder.OpenBlock($"public class {reducerName} : IReducerFor<{modelType}>");
        foreach (var transitionEvent in reducerEvents)
        {
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(transitionEvent.Id).Path);
            var eventType = $"global::{eventNamespace}.{Identifiers.ToPascalCase(transitionEvent.Name)}";
            builder.OpenBlock($"public {modelType}? On({eventType} @event, {modelType}? current, EventContext eventContext)")
                .RawVerbatim("throw new global::System.NotImplementedException();")
                .EndBlock();
        }

        var code = builder.OpenBlock($"static {modelType}? Transition_{suffix}({wrapperType} context)")
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
            ReferenceDirectoryOverride.Value is { } referenceDirectory ? LoadReferences(referenceDirectory) : _references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var semanticModel = compilation.GetSemanticModel(tree);
        var referencedTypes = MethodTypeReferences(tree, semanticModel);
        var shadow = peers.FirstOrDefault(name => referencedTypes.Any(reference => reference.Name == name) || ShadowsAuditedName(name));
        if (shadow is not null)
            return Reject("STAGE-ESM-022", $"Generated type '{shadow}' shadows a type referenced by the reducer body.");
        var namespaceShadow = namespacePaths.FirstOrDefault(entry => ShadowsAuditedName(entry.Key) ||
            referencedTypes.Any(reference => reference.Name == entry.Key &&
                (reference.Namespace is null || entry.Value.Any(path => path != reference.Namespace)))).Key;
        if (namespaceShadow is not null)
            return Reject("STAGE-ESM-022", $"Generated namespace '{namespaceShadow}' shadows a type referenced by the reducer body.");

        // These type names are used by the emitted method even when absent from the body.
        var runtimeShadow = peers.FirstOrDefault(name => NameIs(name, "EventContext", "ReducerContextValues", "IReducerFor"));
        if (runtimeShadow is not null)
            return Reject("STAGE-ESM-022", $"Generated type '{runtimeShadow}' shadows a reducer runtime type.");
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            var error = errors[0];
            var methodOffset = code.IndexOf(body, code.LastIndexOf($"public class {reducerName}", StringComparison.Ordinal), StringComparison.Ordinal);
            var detail = $"{error.Id}: {error.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}{GeneratedMemberDiagnostic.Hint(error, semanticModel)}";
            if (!error.Location.IsInSource || error.Location.SourceTree != tree || error.Location.SourceSpan.Start < methodOffset ||
                error.Location.SourceSpan.Start > methodOffset + body.Length)
            {
                return Reject("STAGE-ESM-021", $"Internal reducer analysis compilation error: {detail}");
            }

            var location = SourceLocation(requirement, body, error.Location.SourceSpan.Start - methodOffset);
            return Reject("STAGE-ESM-019", $"Reducer body {location}: {detail}");
        }

        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(_ => _.Identifier.Text == $"Transition_{suffix}");
        return Walk(semanticModel, method.Body!, descriptor);

        string Parameters(IEnumerable<SemanticProperty> properties, bool reducerInput = false) => string.Join(", ", properties.Select(property =>
            $"{types.Type(property.Type, reducerInput)} {Identifiers.ToPascalCase(property.Name)}"));
    }

    // Shared by every pure body role: the text must be one complete block without directives,
    // CR or forbidden syntax, and may type-test only the named generated types.
    internal static (BlockSyntax? Block, Verdict? Rejection) ParseBody(string body, IReadOnlySet<string> generatedNames, string role)
    {
        // PlannedArtifact.CreateText normalizes CR even inside verbatim literals. Refuse it so
        // the published body is exactly the text analysed here.
        if (body.Contains('\r')) return (null, Reject("STAGE-ESM-022", $"Carriage return in {role} body changes emitted text."));
        var text = "{\n" + body + "\n}";
        var statement = SyntaxFactory.ParseStatement(text, consumeFullText: true);
        if (statement.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsDirective))
        {
            return (null, Reject("STAGE-ESM-022", "directive"));
        }

        if (statement is not BlockSyntax block || block.ContainsDiagnostics || block.ContainsSkippedText ||
            block.CloseBraceToken.IsMissing || block.CloseBraceToken.SpanStart != text.Length - 1)
        {
            return (null, Reject("STAGE-ESM-019", $"The {role} body is not a complete C# statement block."));
        }

        var forbidden = block.DescendantNodesAndSelf().FirstOrDefault(node =>
            node is AwaitExpressionSyntax or YieldStatementSyntax or LockStatementSyntax or UnsafeStatementSyntax or
                FixedStatementSyntax or StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax or
                PointerTypeSyntax or FunctionPointerTypeSyntax or InterpolatedStringExpressionSyntax or
                LocalFunctionStatementSyntax or WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax or
                GotoStatementSyntax or TryStatementSyntax or UsingStatementSyntax or
                TypeOfExpressionSyntax or SizeOfExpressionSyntax or AnonymousObjectCreationExpressionSyntax or
                CollectionExpressionSyntax or RecursivePatternSyntax or ListPatternSyntax or RelationalPatternSyntax or
                ParenthesizedLambdaExpressionSyntax { AttributeLists.Count: > 0 } or
                SimpleLambdaExpressionSyntax { AttributeLists.Count: > 0 } or
                ParameterSyntax { AttributeLists.Count: > 0 } or
                AnonymousFunctionExpressionSyntax { AsyncKeyword.RawKind: not 0 } or
                RefExpressionSyntax or RefTypeSyntax or RangeExpressionSyntax or
                LocalDeclarationStatementSyntax { UsingKeyword.RawKind: not 0 } ||
                node.IsKind(SyntaxKind.CoalesceAssignmentExpression));
        if (forbidden is not null)
        {
            var symbol = forbidden switch
            {
                LocalFunctionStatementSyntax local => local.Identifier.Text,
                ParameterSyntax parameter => parameter.Identifier.Text,
                AnonymousFunctionExpressionSyntax => "lambda",
                RecursivePatternSyntax when body.Contains("context", StringComparison.Ordinal) => "TypedContext_",
                _ => forbidden.Kind().ToString()
            };
            return (null, Reject("STAGE-ESM-022", $"Forbidden pure construct '{forbidden.Kind()}' on '{symbol}'."));
        }

        var disallowedTest = block.DescendantNodes().OfType<PatternSyntax>().FirstOrDefault(pattern => pattern switch
        {
            DeclarationPatternSyntax declaration => !generatedNames.Contains(declaration.Type.ToString()),
            TypePatternSyntax typePattern => !generatedNames.Contains(typePattern.Type.ToString()),
            _ => false
        });
        if (disallowedTest is not null)
        {
            return (null, Reject("STAGE-ESM-022", $"Type test '{disallowedTest}' is outside the pure allowlist; only generated types are admitted."));
        }

        return (block, null);
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

    internal static bool ShadowsAuditedName(string name) =>
        NameIs(name, "Math", "String", "Enumerable", "ImmutableArray", "DateTimeOffset", "TimeSpan", "StringComparison", "MidpointRounding", "CultureInfo", "ArgumentException", "InvalidOperationException", "IEnumerable", "IFormatProvider", "Guid", "DateOnly", "EventContext", "ReducerContextValues");

    internal static HashSet<(string Name, string? Namespace)> MethodTypeReferences(SyntaxTree tree, SemanticModel model, string methodPrefix = "Transition_")
    {
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(declaration => declaration.Identifier.Text.StartsWith(methodPrefix, StringComparison.Ordinal));
        var names = new HashSet<(string Name, string? Namespace)>();
        foreach (var name in method.Body!.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            var info = model.GetSymbolInfo(name);

            // Qualifiers are shadowable even when the stub binds a generated namespace. Include
            // the right-hand side of qualified names, not just their syntactic root.
            if (info.Symbol is INamespaceSymbol ns)
            {
                names.Add((name.Identifier.ValueText, ns.ToDisplayString()));
            }
            else if (info.Symbol is INamedTypeSymbol ||
                (info.Symbol is null && info.CandidateSymbols.IsEmpty &&
                 name.Parent is MemberAccessExpressionSyntax { Expression: var expression } && expression == name))
            {
                names.Add((name.Identifier.ValueText, null));
            }
        }

        return names;
    }

    internal static Verdict Walk(SemanticModel model, BlockSyntax body, SemanticTypedContextDescriptor descriptor)
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

            if (operation is IObjectCreationOperation constructedException &&
                NameIs(
                    constructedException.Constructor?.ContainingType.ToDisplayString() ?? string.Empty,
                    "System.InvalidOperationException",
                    "System.ArgumentException") &&
                !IsThrownCreation(constructedException))
            {
                return Reject("STAGE-ESM-022", "Exception construction is admitted only inside a throw expression.");
            }

            if (operation is IThrowOperation thrownOperation && !ValidThrow(thrownOperation.Exception))
                return Reject("STAGE-ESM-022", "Throw must construct InvalidOperationException(string) or ArgumentException(string).");

            if (operation is IAnonymousFunctionOperation lambda && InvalidLambda(lambda))
            {
                return Reject("STAGE-ESM-022", "LINQ lambda must be a pure expression over its parameters and typed context (no writes, deferred captures, nested LINQ or evaluation-dependent throws).");
            }

            if (operation is IInvocationOperation linq && linq.TargetMethod.ContainingType.ToDisplayString() == "System.Linq.Enumerable" &&
                ((NameIs(linq.TargetMethod.Name, "Any", "Count") && linq.TargetMethod.Parameters.Length == 1) ||
                 linq.Arguments.Any(argument => HasDeferredLinqSource(argument.Value, root))))
            {
                return Reject("STAGE-ESM-022", "LINQ count shortcuts and deferred LINQ composition are outside the pure subset.");
            }

            if (operation is IInvocationOperation immutable && immutable.TargetMethod.ContainingType.ToDisplayString() == "System.Collections.Immutable.ImmutableArray" &&
                immutable.Arguments.Any(argument => HasDeferredLinqSource(argument.Value, root)))
            {
                return Reject("STAGE-ESM-022", "Materializing a deferred LINQ source is outside the pure subset.");
            }

            if (operation is ISimpleAssignmentOperation { Target: ILocalReferenceOperation assigned } && EnumerableType(assigned.Local.Type))
            {
                return Reject("STAGE-ESM-022", $"Enumerable local '{assigned.Local.Name}' must be single-assignment.");
            }

            if (operation is ILiteralOperation { Type: { } literalType } && !SafeDataType(literalType, model.Compilation))
                return Reject("STAGE-ESM-022", $"Literal of '{literalType}' is outside the pure allowlist.");
            if (operation is IVariableDeclaratorOperation declarator &&
                !SafeDataType(declarator.Symbol.Type, model.Compilation) &&
                !(declarator.Symbol.Type is INamedTypeSymbol variableType &&
                  variableType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>" &&
                  SafeDataType(variableType.TypeArguments[0], model.Compilation)))
            {
                return Reject("STAGE-ESM-022", $"Local '{declarator.Symbol.Name}' has a type outside the pure allowlist.");
            }

            if (operation is IBinaryOperation binaryOperation && !SafeBinary(binaryOperation, model.Compilation))
                return Reject("STAGE-ESM-022", $"Binary operation '{binaryOperation.OperatorKind}' on '{binaryOperation.LeftOperand.Type}' is outside the pure allowlist.");
            if (operation is IUnaryOperation unaryOperation && !SafeValue(unaryOperation.Operand.Type, model.Compilation))
                return Reject("STAGE-ESM-022", $"Unary operation on '{unaryOperation.Operand.Type}' is outside the pure allowlist.");
            if (operation is ICompoundAssignmentOperation compoundOperation &&
                !(compoundOperation.Type?.SpecialType == SpecialType.System_String && StringOrChar(compoundOperation.Value)) &&
                !SafeValue(compoundOperation.Value.Type, model.Compilation))
            {
                return Reject("STAGE-ESM-022", $"Compound assignment on '{compoundOperation.Value.Type}' is outside the pure allowlist.");
            }

            if (operation is IIncrementOrDecrementOperation incrementValue && !Number(incrementValue.Target.Type))
                return Reject("STAGE-ESM-022", $"Increment on '{incrementValue.Target.Type}' is outside the pure allowlist.");
            if (operation is IFieldReferenceOperation enumField &&
                NameIs(enumField.Field.ContainingType.ToDisplayString(), "System.StringComparison", "System.MidpointRounding") &&
                !AuditedEnumArgument(enumField))
            {
                return Reject("STAGE-ESM-022", $"Enum field '{enumField.Field.ToDisplayString()}' is admitted only as an audited method argument.");
            }

            if (operation is IPropertyReferenceOperation provider && provider.Property.Name == "InvariantCulture" &&
                IsInvariantProvider(provider) &&
                ArgumentFor(provider) is not IArgumentOperation { Parent: IInvocationOperation { TargetMethod.Name: "ToString" } })
            {
                return Reject("STAGE-ESM-022", "CultureInfo.InvariantCulture is admitted only for numeric ToString.");
            }

            if (operation is IPropertyReferenceOperation propertyValue &&
                !SafeDataType(propertyValue.Type, model.Compilation) &&
                !(propertyValue.Property.Name == "InvariantCulture" && IsInvariantProvider(propertyValue)))
            {
                return Reject("STAGE-ESM-022", $"Property '{propertyValue.Property.ToDisplayString()}' returns a type outside the pure allowlist.");
            }

            if (operation is IConversionOperation conversionOperation &&
                ((conversionOperation.Syntax is CastExpressionSyntax &&
                  !(Number(conversionOperation.Operand.Type) && Number(conversionOperation.Type) &&
                    (WideningInteger(conversionOperation.Operand.Type, conversionOperation.Type) ||
                     conversionOperation.Operand.Type?.SpecialType == SpecialType.System_Decimal ||
                     conversionOperation.Type?.SpecialType == SpecialType.System_Decimal))) ||
                 !SafeConversion(conversionOperation)))
            {
                return Reject("STAGE-ESM-022", $"Conversion to '{conversionOperation.Type?.ToDisplayString()}' is outside the pure allowlist.");
            }
            if (operation is IIsPatternOperation pattern && !SafePattern(pattern.Pattern, model.Compilation))
                return Reject("STAGE-ESM-022", $"Pattern '{pattern.Pattern.Syntax}' is outside the pure allowlist.");
            if (operation is IDefaultValueOperation defaultValue && !Generated(defaultValue.Type, model.Compilation))
            {
                return Reject("STAGE-ESM-022", $"Default of '{defaultValue.Type?.ToDisplayString()}' is outside the pure allowlist.");
            }

            if (operation is IArrayCreationOperation arrayCreation && arrayCreation.Type is IArrayTypeSymbol arrayType &&
                !SafeValue(arrayType.ElementType, model.Compilation))
            {
                return Reject("STAGE-ESM-022", $"Array element type '{arrayType.ElementType}' is outside the pure allowlist.");
            }

            if (operation is IWithOperation withOperation && !Generated(withOperation.Type, model.Compilation))
                return Reject("STAGE-ESM-022", $"With on '{withOperation.Type?.ToDisplayString()}' is outside the pure allowlist.");
            if (operation is IArrayElementReferenceOperation element && element.Parent is ISimpleAssignmentOperation or ICompoundAssignmentOperation or IIncrementOrDecrementOperation)
                return Reject("STAGE-ESM-022", "Array element mutation is outside the pure allowlist.");
            if (operation is ICompoundAssignmentOperation compoundString && compoundString.Type?.SpecialType == SpecialType.System_String &&
                (!StringOrChar(compoundString.Value) || compoundString.Target.Type?.SpecialType != SpecialType.System_String))
            {
                return Reject("STAGE-ESM-022", "Culture-sensitive string.Concat operands are not pure.");
            }

            if (operation is IInvocationOperation concatCall && concatCall.TargetMethod.Name == "Concat" &&
                concatCall.TargetMethod.ContainingType.SpecialType == SpecialType.System_String &&
                concatCall.Arguments.Any(argument => !StringOrChar(argument.Value)))
            {
                return Reject("STAGE-ESM-022", "Symbol 'string.Concat' requires string or char operands.");
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
                ICompoundAssignmentOperation { OperatorMethod: { } compoundOperator } => compoundOperator,
                IForEachLoopOperation loop when loop.Syntax is CommonForEachStatementSyntax syntax =>
                    model.GetForEachStatementInfo(syntax).GetEnumeratorMethod,
                IDeconstructionAssignmentOperation { Syntax: AssignmentExpressionSyntax deconstruction } =>
                    model.GetDeconstructionInfo(deconstruction).Method,
                IRecursivePatternOperation recursive => recursive.DeconstructSymbol,
                _ => null
            };
            if ((operation is IDeconstructionAssignmentOperation && symbol is null) ||
                (operation is IForEachLoopOperation && symbol is null && !IsArrayForeach(operation, model)) ||
                (operation is IRecursivePatternOperation { DeconstructionSubpatterns.Length: > 0 } && symbol is null))
            {
                return Reject("STAGE-ESM-022", $"Unresolved operation '{operation.Kind}' is outside the pure allowlist.");
            }

            if (symbol is null) continue;
            if (IsArrayForeach(operation, model))
            {
                used.Add("collections");
                continue;
            }

            if (operation is IForEachLoopOperation { Syntax: CommonForEachStatementSyntax forEach })
            {
                var info = model.GetForEachStatementInfo(forEach);
                if (info.GetEnumeratorMethod?.ContainingType.ToDisplayString().StartsWith("System.Collections.Immutable.ImmutableArray<", StringComparison.Ordinal) != true ||
                    info.MoveNextMethod is null || info.CurrentProperty is null ||
                    !AuditedMemberSignatures.Contains(AuditedSignature(info.MoveNextMethod)) ||
                    !AuditedMemberSignatures.Contains(AuditedSignature(info.CurrentProperty)))
                {
                    return Reject("STAGE-ESM-022", "Foreach requires audited immutable-array GetEnumerator, MoveNext and Current bindings.");
                }
            }

            if ((operation is ICompoundAssignmentOperation { Type.SpecialType: SpecialType.System_String } compoundText &&
                 StringOrChar(compoundText.Value)) ||
                (operation is IBinaryOperation { Type.SpecialType: SpecialType.System_String, OperatorKind: BinaryOperatorKind.Add } binaryText &&
                 StringOrChar(binaryText.LeftOperand) && StringOrChar(binaryText.RightOperand)))
            {
                used.Add("string");
                continue;
            }

            if (operation is IInvocationOperation invoked &&
                (!ValidInvocation(invoked) || (invoked.TargetMethod.Name == "Concat" && invoked.TargetMethod.ContainingType.SpecialType == SpecialType.System_String &&
                 invoked.Arguments.Any(argument => !StringOrChar(argument.Value)))))
            {
                return Reject("STAGE-ESM-022", $"Symbol '{symbol.ToDisplayString()}' has unaudited arguments.");
            }

            if (operation is IPropertyReferenceOperation propertyRead && propertyRead.Property.ContainingType.Name == wrapper)
            {
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

            if (IsAuditedMember(symbol) && !AuditedMemberSignatures.Contains(AuditedSignature(symbol)))
            {
                return Reject("STAGE-ESM-022", $"Symbol '{symbol.ToDisplayString()}' has no audited signature fixture.");
            }

            used.Add(entry);
        }

        return new(
            null,
            null,
            reads.ToImmutable(),
            used.ToImmutable(),
            [.. Descendants(root).SelectMany(operation => BoundMembers(operation, model)).Select(SymbolIdentity)],
            [.. Descendants(root).SelectMany(operation => BoundMembers(operation, model)).Where(IsAuditedMember).Select(AuditedSignature).Distinct(StringComparer.Ordinal)]);
    }

    internal static string SourceLocation(SemanticImplementationRequirement requirement, string body, int offset)
    {
        var line = body.AsSpan(0, offset).Count('\n');
        var start = body.LastIndexOf('\n', Math.Max(0, offset - 1));
        var column = offset - start;
        if (requirement.File is not null) return $"{requirement.File}:{line + 1}:{column}";
        if (!requirement.BodyLines.IsDefaultOrEmpty && line < requirement.BodyLines.Length)
            return $"{requirement.Source.Span.Document}:{requirement.BodyLines[line].Line}:{requirement.BodyLines[line].Column + column - 1}";
        return $"line {requirement.BodySpan?.StartLine + line}, column {column}";
    }

    // The audited pack, or the isolated failure-path override, for every pure body analysis.
    internal static MetadataReference[] AnalysisReferences() =>
        ReferenceDirectoryOverride.Value is { } referenceDirectory ? LoadReferences(referenceDirectory) : _references.Value;

    static MetadataReference[] LoadEmbeddedReferences()
    {
        var assembly = typeof(PureTransitionAdmission).Assembly;
        var resources = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith($"PureTransitionReferences.{ReferencePackVersion}.", StringComparison.Ordinal) && name.EndsWith(".dll", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).ToArray();
        if (resources.Length == 0)
            throw new InvalidOperationException($"Reference assemblies {ReferencePackVersion} for target framework 'net10.0' are unavailable; pure transitions cannot be analysed.");
        return [.. resources.Select(name =>
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            return MetadataReference.CreateFromStream(stream, filePath: name);
        })];
    }

    static MetadataReference[] LoadReferences(string refDirectory)
    {
        var framework = CratisBackendApplicationScaffoldProfile.Current.TargetFramework;
        var paths = Directory.Exists(refDirectory) ? Directory.GetFiles(refDirectory, "*.dll") : [];
        if (paths.Length == 0)
            throw new InvalidOperationException($"Reference assemblies {ReferencePackVersion} for target framework '{framework}' are unavailable; pure transitions cannot be analysed.");
        return [.. paths.Select(path => MetadataReference.CreateFromFile(path))];
    }

    static IEnumerable<ISymbol> BoundMembers(IOperation operation, SemanticModel model)
    {
        if (operation is IForEachLoopOperation { Syntax: CommonForEachStatementSyntax syntax } && !IsArrayForeach(operation, model))
        {
            var info = model.GetForEachStatementInfo(syntax);
            if (info.GetEnumeratorMethod is not null) yield return info.GetEnumeratorMethod;
            if (info.MoveNextMethod is not null) yield return info.MoveNextMethod;
            if (info.CurrentProperty is not null) yield return info.CurrentProperty;
        }

        ISymbol? symbol = operation switch
        {
            IInvocationOperation call => call.TargetMethod,
            IObjectCreationOperation creation => creation.Constructor,
            IPropertyReferenceOperation property => property.Property,
            IFieldReferenceOperation field => field.Field,
            IBinaryOperation { OperatorMethod: { } binary } => binary,
            IUnaryOperation { OperatorMethod: { } unary } => unary,
            ICompoundAssignmentOperation { OperatorMethod: { } compound } => compound,
            IConversionOperation { OperatorMethod: { } conversion } => conversion,
            _ => null
        };
        if (symbol is not null) yield return symbol;
    }

    static bool InvalidLambda(IAnonymousFunctionOperation lambda)
    {
        if (lambda.Parent is not IDelegateCreationOperation { Parent: IArgumentOperation { Parent: IInvocationOperation call } } ||
            call.TargetMethod.ContainingType.ToDisplayString() != "System.Linq.Enumerable" ||
            lambda.Syntax is not LambdaExpressionSyntax { Body: ExpressionSyntax })
        {
            return true;
        }

        return lambda.Syntax.DescendantNodes().OfType<CheckedExpressionSyntax>().Any() ||
            Descendants(lambda.Body).Any(child => child != lambda.Body && InvalidLambdaChild(child, lambda));
    }

    static bool InvalidLambdaChild(IOperation child, IAnonymousFunctionOperation lambda) => child switch
    {
        ISimpleAssignmentOperation or ICompoundAssignmentOperation or IIncrementOrDecrementOperation or
            ICoalesceAssignmentOperation or IThrowOperation or IAnonymousFunctionOperation or IArrayElementReferenceOperation => true,
        IBinaryOperation binary => binary.IsChecked ||
            (binary.OperatorKind is BinaryOperatorKind.Divide or BinaryOperatorKind.Remainder) ||
            (binary.Type?.SpecialType == SpecialType.System_Decimal &&
             binary.OperatorKind is BinaryOperatorKind.Add or BinaryOperatorKind.Subtract or BinaryOperatorKind.Multiply),
        IUnaryOperation unary => unary.IsChecked ||
            (unary.Operand.Type?.SpecialType == SpecialType.System_Decimal && unary.OperatorKind == UnaryOperatorKind.Minus),
        IConversionOperation conversion => conversion.IsChecked ||
            (conversion.Operand.Type?.SpecialType == SpecialType.System_Decimal && conversion.Type?.SpecialType != SpecialType.System_Decimal),
        IInvocationOperation call => !SafeLambdaCall(call),
        ILocalReferenceOperation or IParameterReferenceOperation => CapturesDisallowedValue(child, lambda),
        _ => false
    };

    static bool EnumerableType(ITypeSymbol? type) => type is not null &&
        (type is IArrayTypeSymbol || type.ToDisplayString().StartsWith("System.Collections.Immutable.ImmutableArray<", StringComparison.Ordinal) ||
         type.AllInterfaces.Any(@interface => @interface.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>") ||
         type.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>");

    static bool CapturesDisallowedValue(IOperation operation, IAnonymousFunctionOperation lambda) => operation switch
    {
        ILocalReferenceOperation => true, // Only lambda parameters and the typed context may be captured.
        IParameterReferenceOperation parameter => !lambda.Symbol.Parameters.Any(p => SymbolEqualityComparer.Default.Equals(p, parameter.Parameter)) &&
            !(parameter.Parameter.Type.Name.StartsWith("TypedContext_", StringComparison.Ordinal) &&
              parameter.Parameter.ContainingSymbol is IMethodSymbol),
        _ => false
    };

    static bool SafeLambdaCall(IInvocationOperation call) =>
        call.TargetMethod.ContainingType.SpecialType == SpecialType.System_String &&
        NameIs(call.TargetMethod.Name, "Equals", "StartsWith", "EndsWith", "Contains", "IndexOf", "IsNullOrEmpty");

    static bool HasDeferredLinqSource(IOperation source, IOperation root)
    {
        while (source is IConversionOperation conversion) source = conversion.Operand;
        if (source is IInvocationOperation call && call.TargetMethod.ContainingType.ToDisplayString() == "System.Linq.Enumerable") return true;
        if (source is ILocalReferenceOperation local)
        {
            return Descendants(root).OfType<IVariableDeclaratorOperation>().Any(declaration =>
                SymbolEqualityComparer.Default.Equals(declaration.Symbol, local.Local) && declaration.Initializer is not null &&
                HasDeferredLinqSource(declaration.Initializer.Value, root));
        }
        return source.ChildOperations.Any(child => HasDeferredLinqSource(child, root));
    }

    static bool IsThrownCreation(IOperation creation)
    {
        var parent = creation.Parent;
        while (parent is IConversionOperation { OperatorMethod: null }) parent = parent.Parent;
        return parent is IThrowOperation;
    }

    static bool ValidThrow(IOperation? exception)
    {
        while (exception is IConversionOperation { OperatorMethod: null } conversion) exception = conversion.Operand;
        return exception is IObjectCreationOperation { Constructor: { } constructor } &&
            NameIs(constructor.ContainingType.ToDisplayString(), "System.InvalidOperationException", "System.ArgumentException") &&
            constructor.Parameters.Length == 1 && constructor.Parameters[0].Type.SpecialType == SpecialType.System_String;
    }

    static bool AuditedEnumArgument(IFieldReferenceOperation field)
    {
        if (ArgumentFor(field) is not IArgumentOperation { Parent: IInvocationOperation call }) return false;
        if (field.Field.ContainingType.Name == "StringComparison")
            return call.TargetMethod.ContainingType.SpecialType == SpecialType.System_String;
        return call.TargetMethod.ContainingType.ToDisplayString() == "System.Math" && call.TargetMethod.Name == "Round";
    }

    static IOperation? ArgumentFor(IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is IConversionOperation { OperatorMethod: null }) parent = parent.Parent;
        return parent;
    }

    static bool IsArrayForeach(IOperation operation, SemanticModel model) =>
        operation is IForEachLoopOperation { Syntax: ForEachStatementSyntax { Expression: { } expression } } &&
        model.GetTypeInfo(expression).Type is IArrayTypeSymbol;

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
        if (symbol is IMethodSymbol { MethodKind: MethodKind.BuiltinOperator } builtin)
        {
            entry = "primitive";
            return builtin.Parameters.All(parameter => SafeValueType(parameter.Type)) && SafeValueType(builtin.ReturnType);
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

        // The pinned ConceptAs<T> and EventSourceId<T> expose auto-property getters only.
        // Mirror their declaring types in the synthetic compilation, rather than admitting a
        // synthetic owned Value that binds differently from the real inherited property.
        var genericOwner = type.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (((genericOwner == "global::Cratis.Concepts.ConceptAs<T>" && name == "Value") ||
             (genericOwner == "global::Cratis.Chronicle.Events.EventSourceId<T>" && name == "TypedValue")) &&
            symbol is IPropertySymbol { IsStatic: false, IsIndexer: false, GetMethod: not null } conceptProperty &&
            SymbolEqualityComparer.Default.Equals(conceptProperty.Type, type.TypeArguments[0]) &&
            SafeValue(conceptProperty.Type, compilation))
        {
            entry = "generated";
            return true;
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
            if (full == $"global::{rootNamespace}.TypedContexts.TenantId" &&
                ((symbol is IFieldSymbol { IsStatic: true, IsReadOnly: true } && NameIs(name, "Default", "NotSet")) ||
                 (symbol is IMethodSymbol { IsImplicitlyDeclared: true, MethodKind: MethodKind.UserDefinedOperator } && NameIs(name, "op_Equality", "op_Inequality"))))
            {
                return true;
            }

            // Stage's policy Identity mirrors Screenplay's audited lookups: ordinal roles, ordinal-ignore-case claim names.
            if (full == $"global::{rootNamespace}.TypedContexts.{SemanticPolicyContextRuntime.IdentityType}" &&
                symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary, IsStatic: false, Parameters.Length: 1 } lookup &&
                lookup.Parameters[0].Type.SpecialType == SpecialType.System_String &&
                NameIs(name, "HasRole", "HasClaim", "ClaimValue", "ClaimValues"))
            {
                return true;
            }

            return name != "ToString" &&
                (type.Name.StartsWith("TypedContext_", StringComparison.Ordinal)
                    ? symbol is IPropertySymbol
                    : symbol is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.PropertyGet } or
                        IPropertySymbol or IFieldSymbol { IsConst: true });
        }

        if (SafeValueType(type))
        {
            if (type.SpecialType == SpecialType.System_String)
            {
                entry = "string";
                if (symbol is IPropertySymbol textProperty) return name == "Length" || textProperty.IsIndexer;
                if (symbol is IFieldSymbol) return name == "Empty";
                return symbol is IMethodSymbol stringMethod &&
                    ((name == "Substring" && stringMethod.Parameters.Length is 1 or 2 &&
                      stringMethod.Parameters.All(parameter => parameter.Type.SpecialType == SpecialType.System_Int32)) ||
                     NameIs(name, "IsNullOrEmpty", "op_Equality", "op_Inequality") ||
                     (name == "Concat" && stringMethod.Parameters.All(parameter => parameter.Type.SpecialType == SpecialType.System_String)) ||
                     (NameIs(name, "Equals", "StartsWith", "EndsWith", "Contains", "IndexOf") &&
                     stringMethod.Parameters.Any(parameter => parameter.Type.ToDisplayString() == "System.StringComparison")));
            }

            entry = "primitive";
            return SafeValueType(type) && symbol is IMethodSymbol { Parameters.Length: 1 } method &&
                method.Name == "ToString" &&
                method.Parameters[0].Type.Name == "IFormatProvider" &&
                method.Parameters[0].Type.ContainingNamespace.ToDisplayString() == "System";
        }

        if (full.StartsWith("global::System.Nullable<", StringComparison.Ordinal))
        {
            entry = "primitive";
            return SafeValueType(type.TypeArguments[0]) && NameIs(name, "HasValue", "Value", "GetValueOrDefault", ".ctor");
        }

        if (full == "global::System.Math")
        {
            entry = "math";
            return (symbol is IMethodSymbol math && math.Parameters.Length > 0 &&
                math.Parameters.All(parameter => Number(parameter.Type)) && Number(math.ReturnType) &&
                NameIs(name, "Abs", "Min", "Max", "Clamp", "Sign")) ||
                (symbol is IMethodSymbol { Parameters.Length: 2 } rounded && name == "Round" &&
                rounded.Parameters[0].Type.SpecialType == SpecialType.System_Decimal &&
                rounded.Parameters[1].Type.ToDisplayString() == "System.MidpointRounding" &&
                rounded.ReturnType.SpecialType == SpecialType.System_Decimal) ||
                (symbol is IMethodSymbol { Parameters.Length: 1 } decimalMath &&
                NameIs(name, "Truncate", "Floor", "Ceiling") &&
                decimalMath.Parameters[0].Type.SpecialType == SpecialType.System_Decimal &&
                decimalMath.ReturnType.SpecialType == SpecialType.System_Decimal);
        }

        if (NameIs(full, "global::System.DateTimeOffset", "global::System.TimeSpan"))
        {
            entry = "time";
            if (symbol is IPropertySymbol) return NameIs(name, "Year", "Month", "Day", "Hour", "Minute", "Second", "Ticks");
            return symbol is IMethodSymbol method &&
                ((NameIs(name, "op_Equality", "op_Inequality", "op_LessThan", "op_LessThanOrEqual", "op_GreaterThan", "op_GreaterThanOrEqual", "op_Addition", "op_Subtraction") &&
                    method.Parameters.All(parameter => NameIs(parameter.Type.ToDisplayString(), "System.DateTimeOffset", "System.TimeSpan"))) ||
                 (NameIs(name, "Add", "AddDays", "AddHours", "AddMinutes", "AddSeconds", "AddMilliseconds", "AddTicks", "AddMonths", "AddYears") &&
                    method.Parameters.Length == 1 &&
                    (Number(method.Parameters[0].Type) || method.Parameters[0].Type.ToDisplayString() == "System.TimeSpan")));
        }

        if (full == "global::System.StringComparison")
        {
            entry = "string";
            return symbol is IFieldSymbol && name == "Ordinal";
        }

        if (full == "global::System.Globalization.CultureInfo")
        {
            entry = "string";
            return symbol is IPropertySymbol && name == "InvariantCulture";
        }

        if (full == "global::System.Collections.Immutable.ImmutableArray")
        {
            entry = "collections";
            return name == "ToImmutableArray" && symbol is IMethodSymbol { Parameters.Length: 1 } method &&
                method.Parameters[0].Type.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>";
        }

        if (full.StartsWith("global::System.Collections.", StringComparison.Ordinal))
        {
            entry = "collections";

            return (full.StartsWith("global::System.Collections.Immutable.ImmutableArray<", StringComparison.Ordinal) &&
                    (NameIs(name, "Length", "IsDefault", "Empty", "GetEnumerator") ||
                     symbol is IPropertySymbol { IsIndexer: true })) ||
                    (full.StartsWith("global::System.Collections.Generic.IEnumerable<", StringComparison.Ordinal) &&
                    name == "GetEnumerator") ||
                    (full.StartsWith("global::System.Collections.Immutable.ImmutableArray<", StringComparison.Ordinal) &&
                    NameIs(name, "Current", "MoveNext"));
        }

        if (full == "global::System.Linq.Enumerable")
        {
            entry = "linq";
            return NameIs(name, "Where", "Select", "Any", "All", "Count", "First", "FirstOrDefault", "Last", "LastOrDefault", "Take", "Skip", "Sum", "Aggregate", "ToArray", "Concat") &&
                (name != "Aggregate" || symbol is IMethodSymbol { Parameters.Length: 3 }) &&
                (name != "Sum" || (symbol is IMethodSymbol sum && DecimalSumReturn(sum.ReturnType)));
        }

        if (NameIs(full, "global::System.InvalidOperationException", "global::System.ArgumentException"))
        {
            entry = "exceptions";
            return symbol is IMethodSymbol { MethodKind: MethodKind.Constructor, Parameters.Length: 1 } ctor &&
                ctor.Parameters[0].Type.SpecialType == SpecialType.System_String;
        }
        if (full == "global::System.MidpointRounding")
        {
            entry = "math";
            return symbol is IFieldSymbol { IsConst: true } && NameIs(name, "ToEven", "AwayFromZero", "ToZero", "ToNegativeInfinity", "ToPositiveInfinity");
        }

        return false;
    }

    // FullyQualifiedFormat alone omits member signatures; include owner, kind, generic arity and parameters.
    static string SymbolIdentity(ISymbol symbol)
    {
        var member = symbol switch
        {
            IMethodSymbol method => $"`{method.Arity}({string.Join(',', method.Parameters.Select(parameter =>
                $"{parameter.RefKind}:{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}"))}):{method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}",
            IPropertySymbol property => $"({string.Join(',', property.Parameters.Select(parameter =>
                parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}):{property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}",
            IFieldSymbol field => $":{field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}",
            _ => string.Empty
        };
        return $"{symbol.Kind}:{symbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{symbol.Name}{member}";
    }

    static bool DecimalSumReturn(ITypeSymbol type) => type.SpecialType == SpecialType.System_Decimal ||
        (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable &&
         nullable.TypeArguments[0].SpecialType == SpecialType.System_Decimal);

    static bool IsAuditedMember(ISymbol symbol) => symbol.ContainingType is { } type &&
        (SafeValueType(type) || type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith("global::System.", StringComparison.Ordinal)) &&
        symbol is not IMethodSymbol { MethodKind: MethodKind.BuiltinOperator };

    static string AuditedSignature(ISymbol symbol) => SymbolIdentity(symbol switch
    {
        IMethodSymbol method => method.OriginalDefinition,
        IPropertySymbol property => property.OriginalDefinition,
        IFieldSymbol field => field.OriginalDefinition,
        _ => symbol
    });

    static bool SafeDataType(ITypeSymbol? type, Compilation compilation) =>
        SafeValue(type, compilation) ||
        (type is IArrayTypeSymbol array && SafeDataType(array.ElementType, compilation)) ||
        (type is INamedTypeSymbol named &&
        (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
         named.OriginalDefinition.ToDisplayString() == "System.Collections.Immutable.ImmutableArray<T>") &&
        SafeDataType(named.TypeArguments[0], compilation));

    static bool SafeValue(ITypeSymbol? type, Compilation compilation) =>
        SafeValueType(type) || Generated(type, compilation) ||
        NameIs(type?.ToDisplayString() ?? string.Empty, "System.DateTimeOffset", "System.TimeSpan", "System.Guid");

    static bool SafeBinary(IBinaryOperation operation, Compilation compilation)
    {
        var left = operation.LeftOperand.Type;
        var right = operation.RightOperand.Type;
        if (operation.Type?.SpecialType == SpecialType.System_String && operation.OperatorKind == BinaryOperatorKind.Add &&
            StringOrChar(operation.LeftOperand) && StringOrChar(operation.RightOperand))
        {
            return true;
        }

        if ((!SafeValue(left, compilation) && operation.LeftOperand.ConstantValue is not { HasValue: true, Value: null }) ||
            (!SafeValue(right, compilation) && operation.RightOperand.ConstantValue is not { HasValue: true, Value: null }))
        {
            return false;
        }

        if (Number(left) && Number(right)) return true;
        if (left?.SpecialType == SpecialType.System_Boolean && right?.SpecialType == SpecialType.System_Boolean) return true;
        if (operation.Type?.SpecialType == SpecialType.System_String &&
            StringOrChar(operation.LeftOperand) && StringOrChar(operation.RightOperand))
        {
            return true;
        }

        if (NameIs(left?.ToDisplayString() ?? string.Empty, "System.DateTimeOffset", "System.TimeSpan") &&
            NameIs(right?.ToDisplayString() ?? string.Empty, "System.DateTimeOffset", "System.TimeSpan"))
        {
            return true;
        }

        return operation.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals &&
            (SymbolEqualityComparer.Default.Equals(left, right) || left is null || right is null);
    }

    static bool WideningInteger(ITypeSymbol? from, ITypeSymbol? to) => (from?.SpecialType, to?.SpecialType) switch
    {
        (SpecialType.System_SByte, SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64) => true,
        (SpecialType.System_Byte, SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64) => true,
        (SpecialType.System_Int16, SpecialType.System_Int32 or SpecialType.System_Int64) => true,
        (SpecialType.System_UInt16, SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64) => true,
        (SpecialType.System_Int32, SpecialType.System_Int64) => true,
        (SpecialType.System_UInt32, SpecialType.System_Int64 or SpecialType.System_UInt64) => true,
        _ => false
    };

    static bool Number(ITypeSymbol? type) => type?.SpecialType is
        SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16 or
        SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or
        SpecialType.System_Decimal;

    static bool SafeValueType(ITypeSymbol? type) => Number(type) || type?.SpecialType is SpecialType.System_Boolean or
        SpecialType.System_Char or SpecialType.System_String;

    static bool Generated(ITypeSymbol? type, Compilation compilation) => type is INamedTypeSymbol named &&
        !named.IsAnonymousType && named.ContainingAssembly.Name == compilation.AssemblyName &&
        named.Locations.Length > 0 && named.Locations.All(location => location.IsInSource &&
            compilation.SyntaxTrees.Contains(location.SourceTree)) && !named.Name.StartsWith("TypedContext_", StringComparison.Ordinal);

    static bool SafeConversion(IConversionOperation operation)
    {
        if (operation.OperatorMethod is not null) return false;
        if (operation.Operand.ConstantValue.HasValue && operation.Operand.ConstantValue.Value is null) return true;
        var from = operation.Operand.Type;
        var to = operation.Type;
        if (from is null || to is null) return false;
        if (operation.Parent is IThrowOperation && operation.Conversion.IsImplicit && ValidThrow(operation.Operand)) return true;
        if (operation.Parent is IForEachLoopOperation && from is IArrayTypeSymbol &&
            to.ToDisplayString() == "System.Collections.IEnumerable")
        {
            return true;
        }

        if (to.Name == "IFormatProvider" && IsInvariantProvider(operation.Operand) && operation.Conversion.IsImplicit) return true;
        if (SymbolEqualityComparer.Default.Equals(from, to)) return true;
        if (from.SpecialType == SpecialType.System_Char && Number(to) && operation.Conversion.IsImplicit) return true;
        if (from.SpecialType == SpecialType.System_Char && to.SpecialType == SpecialType.System_Object &&
            operation.Parent is ICompoundAssignmentOperation { Type.SpecialType: SpecialType.System_String } or
                IBinaryOperation { Type.SpecialType: SpecialType.System_String, OperatorKind: BinaryOperatorKind.Add })
        {
            return true;
        }

        if (Number(from) && Number(to))
        {
            return operation.Conversion.IsImplicit || from.SpecialType == SpecialType.System_Decimal ||
                to.SpecialType == SpecialType.System_Decimal;
        }

        if (from is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable &&
            SymbolEqualityComparer.Default.Equals(nullable.TypeArguments[0], to))
        {
            return operation.Conversion.IsImplicit;
        }

        if (to is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableTarget &&
            SymbolEqualityComparer.Default.Equals(from, nullableTarget.TypeArguments[0]))
        {
            return operation.Conversion.IsImplicit;
        }

        return operation.Conversion.IsImplicit && to is INamedTypeSymbol target &&
            target.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>" &&
            (from is IArrayTypeSymbol || from.ToDisplayString().StartsWith("System.Collections.Immutable.ImmutableArray<", StringComparison.Ordinal));
    }

    static bool SafePattern(IPatternOperation pattern, Compilation compilation) => pattern switch
    {
        IConstantPatternOperation constant => constant.Value.ConstantValue.HasValue &&
            (constant.Value.ConstantValue.Value is null || SafeValueType(constant.Value.Type)),
        INegatedPatternOperation negated => SafePattern(negated.Pattern, compilation),
        IDeclarationPatternOperation declaration => Generated(declaration.MatchedType, compilation),
        IDiscardPatternOperation => true,
        _ => false
    };

    static bool ValidInvocation(IInvocationOperation call)
    {
        var method = call.TargetMethod;
        var owner = method.ContainingType.ToDisplayString();
        if (owner == "System.String" && NameIs(method.Name, "Equals", "StartsWith", "EndsWith", "Contains", "IndexOf"))
        {
            var comparison = call.Arguments.SingleOrDefault(argument => argument.Parameter?.Type.ToDisplayString() == "System.StringComparison");
            if (comparison is null || comparison.Value is not IFieldReferenceOperation field ||
                field.Field.Name != "Ordinal")
            {
                return false;
            }
        }
        if (method.Name == "ToString" && Number(method.ContainingType))
            return call.Arguments.Length == 1 && IsInvariantProvider(call.Arguments[0].Value);
        return owner != "System.Linq.Enumerable" || method.Name != "Sum" || DecimalSumReturn(method.ReturnType);
    }

    static bool NameIs(string name, params string[] names) => names.Contains(name, StringComparer.Ordinal);

    static IEnumerable<IOperation> Descendants(IOperation root)
    {
        yield return root;
        foreach (var child in root.ChildOperations)
            foreach (var nested in Descendants(child)) yield return nested;
    }

    static Verdict Reject(string code, string reason) => new(code, reason, [], []);

    internal sealed record Verdict(string? Code, string? Reason, ImmutableHashSet<string> ContextReads,
        ImmutableHashSet<string> UsedAllowlistEntries, ImmutableArray<string> BoundSymbols = default,
        ImmutableArray<string> AuditedSymbols = default)
    {
        internal bool Accepted => Code is null;
    }
}
