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
internal static partial class PureTransitionAdmission
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
        "Method:global::System.Guid.Parse`0(None:string):global::System.Guid",
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
        ["guid"] = "Exactly Guid.Parse(string); a malformed text key throws at runtime.",
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

        // The actual output file owns the binding environment: a separate classic reducer,
        // or the entire combined slice, including sibling reducers/projections and Automation commands.
        var environment = context.ReducerBindings(located, reducer);
        var reducerEvents = reducer.Transitions.Select(transition => context.Events[transition.EventContract]).ToArray();
        var stubbed = reducerEvents.Select(@event => (
                Id: (SemanticId?)@event.Id,
                Namespace: SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path),
                Name: Identifiers.ToPascalCase(@event.Name)))
            .Append(((SemanticId?)readModel.Id, modelNs, Identifiers.ToPascalCase(readModel.Name)))
            .ToHashSet();
        var allDeclarations = environment.Declarations.Select(declaration =>
            (Id: (SemanticId?)declaration.Artifact, Namespace: SemanticReducerBindingEnvironment.Qualify(context, declaration.Namespace), declaration.Name)).ToArray();
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
                definitions.Add($"namespace {context.CommonNamespace} {{ public enum {Identifiers.ToPascalCase(concept.Name)} {{ {string.Join(", ", SemanticEnumMembers.Declarations(concept))} }} }}");
                continue;
            }

            var scalar = types.Primitive(concept.Primitive);

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

        var self = context.SelfReduces(readModel.Id);
        if (!self)
        {
            definitions.Add($"namespace {modelNs} {{ public record {Identifiers.ToPascalCase(readModel.Name)}({Parameters(SemanticStateViewArtifactRenderer.OrderedProperties(readModel.Properties), true)}); }}");
        }
        foreach (var visibleEvent in environment.Events)
        {
            var eventNs = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(visibleEvent.Id).Path);
            var immutable = context.Reducers.Any(candidate => candidate.Transitions.Any(transition => transition.EventContract == visibleEvent.Id));
            definitions.Add($"namespace {eventNs} {{ public record {Identifiers.ToPascalCase(visibleEvent.Name)}({Parameters(visibleEvent.Properties, immutable)}); }}");
        }
        var defined = environment.Events.Select(visibleEvent =>
                (Namespace: SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(visibleEvent.Id).Path), Name: Identifiers.ToPascalCase(visibleEvent.Name)))
            .Concat(context.Application.Concepts.Select(concept => (Namespace: context.CommonNamespace, Name: Identifiers.ToPascalCase(concept.Name))))
            .Concat(context.Application.Types.Select(type => (Namespace: context.CommonNamespace, Name: Identifiers.ToPascalCase(type.Name))))
            .Append((modelNs, Identifiers.ToPascalCase(readModel.Name))).ToHashSet();
        if (!self) defined.Add((ns, reducerName));
        foreach (var peer in allDeclarations.Where(declaration => !defined.Contains((declaration.Namespace, declaration.Name)))
            .DistinctBy(declaration => (declaration.Namespace, declaration.Name)))
        {
            // Peer members are never an admitted pure API. Their declarations still participate in
            // namespace and short-name binding exactly where the real combined file imports them.
            definitions.Add($"namespace {peer.Namespace} {{ public class {peer.Name} {{}} }}");
        }
        var descriptors = self
            ? context.Request.TypedContextDescriptors.Where(candidate => candidate.OperationId == readModel.Id)
            : [descriptor];
        string wrapper;
        using (CodeGeneration.QualifiedSpelling.Begin())
        {
            wrapper = string.Join('\n', descriptors.Select(candidate => SemanticTypedContextRenderer.Render(candidate, context).Content
                .Replace($"namespace {context.TypedContextsNamespace};", $"namespace {context.TypedContextsNamespace} {{", StringComparison.Ordinal) + "\n}"));
        }

        var modelType = $"global::{modelNs}.{Identifiers.ToPascalCase(readModel.Name)}";
        var wrapperType = $"global::{context.TypedContextsNamespace}.TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}";
        var tenant = $"namespace {context.RootNamespace}.TypedContexts {{ public record TenantId(string Value) {{ public static readonly TenantId Default = new(\"00000000-0000-0000-0000-000000000000\"); public static readonly TenantId NotSet = new(\"\"); }} }}";

        // Model stubs are compilation peers, not imports into the reducer file. The body has the
        // same namespace and exact using directives as SemanticReducerArtifactRenderer.Render.
        // Observable and injected query interfaces are signature-only stand-ins, not admitted pure call surfaces.
        var declarations = "global using System; global using System.Collections.Generic; global using System.Linq; " +
            "global using System.IO; global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks; " +
            (context.Application.Concepts.IsEmpty && context.Application.Types.IsEmpty ? string.Empty : $"using {context.CommonNamespace}; ") +
            "namespace Cratis.Concepts { public record ConceptAs<T>(T Value); } " +
            "namespace Cratis.Chronicle.Events { public class EventContext {} public record EventSourceId(string Value) : global::Cratis.Concepts.ConceptAs<string>(Value); public record EventSourceId<T>(T TypedValue) : global::Cratis.Concepts.ConceptAs<T>(TypedValue); } " +
            "namespace Cratis.Chronicle.Reducers { public interface IReducerFor<T> {} } " +
            "namespace Cratis.Arc.Authorization { public interface ICurrentPrincipalAccessor {} } " +
            "namespace Cratis.Arc.Queries { public interface IQueryContextManager {} } " +
            "namespace Cratis.Arc.Tenancy { public interface ITenantIdAccessor {} } namespace Cratis.Arc.Queries.ModelBound {} " +
            "namespace Cratis.Arc.Commands.ModelBound {} namespace Cratis.Arc.Validation {} namespace Cratis.Chronicle.EventSequences {} " +
            "namespace Cratis.Chronicle.Projections.ModelBound {} namespace Cratis.Chronicle.Keys {} " +
            "namespace Cratis.Chronicle.ReadModels { public interface IReadModels {} } " +
            "namespace System.Reactive.Subjects { public interface ISubject<T> : global::System.IObservable<T>, global::System.IObserver<T> {} } " +
            string.Join('\n', definitions) + "\n" + tenant + "\n" + wrapper;
        var builder = new CodeGeneration.CSharpCodeBuilder().Namespace(ns);
        environment.Configure(builder);

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
        if (self)
        {
            code = SelfAnalysisCode(context, located, readModel, reducer, descriptor, body);
        }
        var options = new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest);
        var tree = CSharpSyntaxTree.ParseText(code, options);
        var declarationTree = CSharpSyntaxTree.ParseText(declarations, options);
        var compilation = CSharpCompilation.Create(
            "PureTransitionAnalysis",
            [declarationTree, tree],
            ReferenceDirectoryOverride.Value is { } referenceDirectory ? LoadReferences(referenceDirectory) : _references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var semanticModel = compilation.GetSemanticModel(tree);
        var referencedTypes = MethodTypeReferences(tree, semanticModel, $"Transition_{suffix}");
        var analyzedBody = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(declaration => IsTransitionMethod(declaration, $"Transition_{suffix}")).Body!;
        var boundTypes = analyzedBody.DescendantNodes().OfType<SimpleNameSyntax>()
            .Select(name => semanticModel.GetSymbolInfo(name).Symbol).OfType<INamedTypeSymbol>().ToArray();
        var shadow = peers.FirstOrDefault(name => ShadowsAuditedName(name) ||
            (referencedTypes.Any(reference => reference.Name == name) &&
                !IsUnambiguousCommonType(name, context, compilation, allDeclarations, boundTypes)));
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
            var helper = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => IsTransitionMethod(method, $"Transition_{suffix}"));
            var methodOffset = code.IndexOf(body, helper.Body!.OpenBraceToken.Span.End, StringComparison.Ordinal);
            var detail = $"{error.Id}: {error.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}{GeneratedMemberDiagnostic.Hint(error, semanticModel)}";
            if (!error.Location.IsInSource || error.Location.SourceTree != tree || error.Location.SourceSpan.Start < methodOffset ||
                error.Location.SourceSpan.Start > methodOffset + body.Length)
            {
                return Reject("STAGE-ESM-021", $"Internal reducer analysis compilation error: {detail}");
            }

            var location = SourceLocation(requirement, body, error.Location.SourceSpan.Start - methodOffset);
            return Reject("STAGE-ESM-019", $"Reducer body {location}: {detail}");
        }

        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => IsTransitionMethod(method, $"Transition_{suffix}"));
        return Walk(semanticModel, method.Body!, descriptor, context.RootNamespace);

        string Parameters(IEnumerable<SemanticProperty> properties, bool reducerInput = false) => string.Join(", ", properties.Select(property =>
            $"{types.Type(property.Type, reducerInput)} {Identifiers.ToPascalCase(property.Name)}"));
    }

    // Every body must remain one complete block without directives or CR. Pure roles additionally
    // refuse forbidden syntax and type tests outside the named generated types.
    internal static (BlockSyntax? Block, Verdict? Rejection) ParseBody(string body, IReadOnlySet<string> generatedNames, string role, bool enforcePurity = true, bool validationIterator = false)
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

        if (statement is not BlockSyntax block || !IsCompleteBlock(block, text))
        {
            return (null, Reject("STAGE-ESM-019", $"The {role} body is not a complete C# statement block."));
        }

        if (!enforcePurity) return (block, null);

        var forbidden = block.DescendantNodesAndSelf().FirstOrDefault(node => IsForbiddenNode(node) &&
            !(validationIterator && (node.IsKind(SyntaxKind.YieldReturnStatement) || node is InterpolatedStringExpressionSyntax)));
        if (forbidden is not null)
        {
            return (null, Reject("STAGE-ESM-022", $"Forbidden pure construct '{forbidden.Kind()}' on '{ForbiddenSymbol(forbidden, body)}'."));
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
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => IsTransitionMethod(method, methodName));
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
            .Single(declaration => IsTransitionMethod(declaration, methodPrefix));
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
            else if (info.Symbol is INamedTypeSymbol || IsUnboundQualifier(info, name))
            {
                names.Add((name.Identifier.ValueText, null));
            }
        }

        return names;
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

    // Only authored Common concepts/composites can opt out of the conservative name guard.
    // Referenced types (including primitive CLR names) and slice-local peers never opt out.
    static bool IsUnambiguousCommonType(
        string name,
        SemanticApplicationContext context,
        CSharpCompilation compilation,
        IEnumerable<(SemanticId? Id, string Namespace, string Name)> declarations,
        IEnumerable<INamedTypeSymbol> boundTypes)
    {
        if (ShadowsAuditedName(name)) return false;
        var commonIds = context.Application.Concepts.Where(concept => Identifiers.ToPascalCase(concept.Name) == name).Select(concept => concept.Id)
            .Concat(context.Application.Types.Where(type => Identifiers.ToPascalCase(type.Name) == name).Select(type => type.Id)).ToHashSet();
        if (commonIds.Count == 0 || declarations.Any(declaration => declaration.Name == name &&
            (declaration.Namespace != context.CommonNamespace || declaration.Id is not { } id || !commonIds.Contains(id))))
        {
            return false;
        }

        var commonType = compilation.GetTypeByMetadataName($"{context.CommonNamespace}.{name}");
        var references = boundTypes.Where(type => type.Name == name).ToArray();
        if (commonType is null || references.Length == 0 || references.Any(type => !SymbolEqualityComparer.Default.Equals(type, commonType))) return false;

        var imports = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>()).ToArray();

        // Alias/static import lookup is not modeled by this narrow exemption.
        if (imports.Any(import => import.Alias is not null || import.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))) return false;
        var namespaces = imports.Select(import => import.Name?.ToString().Replace("global::", string.Empty, StringComparison.Ordinal))
            .OfType<string>().Append("System").Append(string.Empty).Distinct(StringComparer.Ordinal);
        foreach (var path in namespaces)
        {
            var imported = compilation.GlobalNamespace;
            foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                imported = imported?.GetNamespaceMembers().FirstOrDefault(candidate => candidate.Name == segment);
            }

            // The merged namespace includes referenced assemblies and signature stand-ins.
            // Ignore only the exact Common declaration being considered, not other source types.
            if (imported?.GetTypeMembers(name).Any(type => !SymbolEqualityComparer.Default.Equals(type, commonType)) == true) return false;
        }

        return true;
    }

    static bool IsTransitionMethod(MethodDeclarationSyntax method, string prefix) =>
        method.Identifier.Text.StartsWith(prefix, StringComparison.Ordinal) ||
        (method.Identifier.Text == SemanticReducerArtifactRenderer.SelfTransitionMethod &&
            method.Parent is ClassDeclarationSyntax container && container.Identifier.Text.StartsWith(prefix, StringComparison.Ordinal));

    static string ForbiddenSymbol(SyntaxNode forbidden, string body) => forbidden switch
    {
        LocalFunctionStatementSyntax local => local.Identifier.Text,
        ParameterSyntax parameter => parameter.Identifier.Text,
        AnonymousFunctionExpressionSyntax => "lambda",
        RecursivePatternSyntax when body.Contains("context", StringComparison.Ordinal) => "TypedContext_",
        _ => forbidden.Kind().ToString()
    };

    static bool IsCompleteBlock(BlockSyntax block, string text) =>
        !block.ContainsDiagnostics && !block.ContainsSkippedText &&
        !block.CloseBraceToken.IsMissing && block.CloseBraceToken.SpanStart == text.Length - 1;

    static bool IsForbiddenNode(SyntaxNode node) =>
        IsForbiddenSyntax(node) || IsForbiddenDecoration(node) || node.IsKind(SyntaxKind.CoalesceAssignmentExpression);

    static bool IsForbiddenSyntax(SyntaxNode node) =>
        node is AwaitExpressionSyntax or YieldStatementSyntax or LockStatementSyntax or UnsafeStatementSyntax or
            FixedStatementSyntax or StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax or
            PointerTypeSyntax or FunctionPointerTypeSyntax or InterpolatedStringExpressionSyntax or
            LocalFunctionStatementSyntax or WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax or
            GotoStatementSyntax or TryStatementSyntax or UsingStatementSyntax or
            TypeOfExpressionSyntax or SizeOfExpressionSyntax or AnonymousObjectCreationExpressionSyntax or
            CollectionExpressionSyntax or RecursivePatternSyntax or ListPatternSyntax or RelationalPatternSyntax or
            RefExpressionSyntax or RefTypeSyntax or RangeExpressionSyntax;

    static bool IsForbiddenDecoration(SyntaxNode node) =>
        node is ParenthesizedLambdaExpressionSyntax { AttributeLists.Count: > 0 } or
            SimpleLambdaExpressionSyntax { AttributeLists.Count: > 0 } or
            ParameterSyntax { AttributeLists.Count: > 0 } or
            AnonymousFunctionExpressionSyntax { AsyncKeyword.RawKind: not 0 } or
            LocalDeclarationStatementSyntax { UsingKeyword.RawKind: not 0 };

    static bool IsUnboundQualifier(SymbolInfo info, SimpleNameSyntax name) =>
        info.Symbol is null && info.CandidateSymbols.IsEmpty &&
        name.Parent is MemberAccessExpressionSyntax { Expression: var expression } && expression == name;

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
            foreach (var member in ForEachMembers(model.GetForEachStatementInfo(syntax)))
            {
                yield return member;
            }
        }

        if (DirectSymbol(operation) is { } symbol) yield return symbol;
    }

    static ISymbol? DirectSymbol(IOperation operation) => operation switch
    {
        IInvocationOperation call => call.TargetMethod,
        IObjectCreationOperation creation => creation.Constructor,
        IPropertyReferenceOperation property => property.Property,
        IFieldReferenceOperation field => field.Field,
        _ => OperatorMethod(operation)
    };

    static IEnumerable<ISymbol> ForEachMembers(ForEachStatementInfo info)
    {
        if (info.GetEnumeratorMethod is not null) yield return info.GetEnumeratorMethod;
        if (info.MoveNextMethod is not null) yield return info.MoveNextMethod;
        if (info.CurrentProperty is not null) yield return info.CurrentProperty;
    }

    static bool InvalidLambda(IAnonymousFunctionOperation lambda)
    {
        if (!IsLinqExpressionLambda(lambda))
        {
            return true;
        }

        return lambda.Syntax.DescendantNodes().OfType<CheckedExpressionSyntax>().Any() ||
            Descendants(lambda.Body).Any(child => child != lambda.Body && InvalidLambdaChild(child, lambda));
    }

    static bool IsLinqExpressionLambda(IAnonymousFunctionOperation lambda) =>
        lambda.Parent is IDelegateCreationOperation { Parent: IArgumentOperation { Parent: IInvocationOperation call } } &&
        call.TargetMethod.ContainingType.ToDisplayString() == "System.Linq.Enumerable" &&
        lambda.Syntax is LambdaExpressionSyntax { Body: ExpressionSyntax };

    static bool InvalidLambdaChild(IOperation child, IAnonymousFunctionOperation lambda) => child switch
    {
        ISimpleAssignmentOperation or ICompoundAssignmentOperation or IIncrementOrDecrementOperation or
            ICoalesceAssignmentOperation or IThrowOperation or IAnonymousFunctionOperation or IArrayElementReferenceOperation => true,
        IBinaryOperation binary => InvalidLambdaBinary(binary),
        IUnaryOperation unary => InvalidLambdaUnary(unary),
        IConversionOperation conversion => InvalidLambdaConversion(conversion),
        _ => InvalidLambdaReference(child, lambda)
    };

    static bool InvalidLambdaReference(IOperation child, IAnonymousFunctionOperation lambda) => child switch
    {
        IInvocationOperation call => !SafeLambdaCall(call),
        ILocalReferenceOperation or IParameterReferenceOperation => CapturesDisallowedValue(child, lambda),
        _ => false
    };

    static bool InvalidLambdaUnary(IUnaryOperation unary) =>
        unary.IsChecked ||
        (unary.Operand.Type?.SpecialType == SpecialType.System_Decimal && unary.OperatorKind == UnaryOperatorKind.Minus);

    static bool InvalidLambdaConversion(IConversionOperation conversion) =>
        conversion.IsChecked ||
        (conversion.Operand.Type?.SpecialType == SpecialType.System_Decimal && conversion.Type?.SpecialType != SpecialType.System_Decimal);

    static bool InvalidLambdaBinary(IBinaryOperation binary) =>
        binary.IsChecked ||
        (binary.OperatorKind is BinaryOperatorKind.Divide or BinaryOperatorKind.Remainder) ||
        (binary.Type?.SpecialType == SpecialType.System_Decimal &&
         binary.OperatorKind is BinaryOperatorKind.Add or BinaryOperatorKind.Subtract or BinaryOperatorKind.Multiply);

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
        if (IsNonOrdinalStringMatch(call, owner))
        {
            return false;
        }

        if (method.Name == "ToString" && Number(method.ContainingType))
            return call.Arguments.Length == 1 && IsInvariantProvider(call.Arguments[0].Value);
        return owner != "System.Linq.Enumerable" || method.Name != "Sum" || DecimalSumReturn(method.ReturnType);
    }

    static bool IsNonOrdinalStringMatch(IInvocationOperation call, string owner) =>
        owner == "System.String" && NameIs(call.TargetMethod.Name, "Equals", "StartsWith", "EndsWith", "Contains", "IndexOf") && !HasOrdinalComparison(call);

    static bool HasOrdinalComparison(IInvocationOperation call) =>
        call.Arguments.SingleOrDefault(argument => argument.Parameter?.Type.ToDisplayString() == "System.StringComparison") is
        { Value: IFieldReferenceOperation field } && field.Field.Name == "Ordinal";

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
