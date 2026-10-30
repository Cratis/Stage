// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Renders and compile-checks the application-local query performer boundary.</summary>
internal static class SemanticQueryContextRuntime
{
    const string ImplicitUsings = "global using System; global using System.Collections.Generic; global using System.IO; global using System.Linq; " +
        "global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks;\n";

    internal static void Render(
        CSharpCodeBuilder builder,
        SemanticKeyedQuery query,
        string model,
        string method,
        string returnType,
        SemanticTypeSystem types,
        SemanticImplementationRequirement requirement,
        string body)
    {
        var arguments = Arguments(query).ToArray();
        var invocation = $"StagePerformer_{method}.Perform(__stageReadModels, __stageContext{string.Concat(arguments.Select(argument => $", {Name(argument)}"))})";
        builder.OpenBlock($"public static async {returnType} {method}({Parameters(query, types)})")
            .Line($"var __stageContext = new StageQueryContext_{method}(new StageQueryArguments_{method}({string.Join(", ", arguments.Select(Name))}), __stageTenantAccessor.Current.Value, __stageQueryContexts.Current.AuthorizedPrincipal ?? __stagePrincipalAccessor.Current ?? new global::System.Security.Claims.ClaimsPrincipal(), __stageQueryContexts.Current.ReceivedAt);")
            .Line($"var __stageReadModels = await __stageReadModelStore.GetInstances<{model}>();")
            .Line($"return {invocation};")
            .EndBlock().BlankLine();
        RenderBody(builder, query, model, method, types, body, requirement.RequirementId);
    }

    internal static void RenderGap(CSharpCodeBuilder builder, SemanticKeyedQuery query, SemanticApplicationContext context, SemanticImplementationAdmission.PerformerAdmission admission)
    {
        var method = context.MemberNames.Query(query);
        var model = Identifiers.ToPascalCase(context.ReadModels[query.ReadModel].Name);
        var returnType = ReturnType(query, model);
        var types = new SemanticTypeSystem(context);
        var source = admission.Requirement?.File is { } file ? $" ({file})" : string.Empty;
        var message = $"Query '{query.Name}' (requirement {admission.Requirement?.RequirementId}){source} is not runnable: {admission.Reason}";
        builder.OpenBlock($"public static {returnType} {method}({Parameters(query, types)})")
            .Line($"throw new global::System.NotImplementedException({CSharpCodeBuilder.StringLiteral(message)});")
            .EndBlock().BlankLine();
        RenderContextDeclarations(builder, query, method, types);
    }

    internal static ArtifactImplementationGap? Gap(SemanticApplicationContext context, SemanticKeyedQuery query, PlannedArtifact artifact, string reason)
    {
        var text = System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
        var methodName = context.MemberNames.Query(query);
        var model = context.ReadModels[query.ReadModel];
        var modelName = Identifiers.ToPascalCase(model.Name);
        var declaration = CSharpSyntaxTree.ParseText(text).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .SingleOrDefault(method => method.Identifier.ValueText == methodName && method.Parent is RecordDeclarationSyntax record && record.Identifier.ValueText == modelName);
        if (declaration?.Body is not { } body) return null;
        var typeName = $"{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(model.Id).Path)}.{modelName}";
        var typedContext = new ArtifactImplementationContext(
            $"{typeName}.StageQueryContext_{methodName}",
            [
                new("Arguments", $"{typeName}.StageQueryArguments_{methodName}", false),
                new("Tenant", "string", false),
                new("Identity", "global::System.Security.Claims.ClaimsPrincipal", false),
                new("ReceivedAt", "global::System.DateTimeOffset", false)
            ]);
        var requirement = context.Request.ImplementationRequirements.First(candidate =>
            candidate.Role == SemanticImplementationRole.QueryPerformer && candidate.Source.SemanticId == query.Id);
        var stub = new ArtifactImplementationStub(
            artifact.RelativePath,
            typeName,
            methodName,
            text[declaration.Modifiers[0].SpanStart..body.OpenBraceToken.SpanStart].TrimEnd(),
            new(body.OpenBraceToken.Span.End, body.CloseBraceToken.SpanStart - body.OpenBraceToken.Span.End),
            typedContext,
            []);
        return new(ArtifactImplementationGapOrigin.Unverified, requirement.RequirementId, query.Id.ToString(), SemanticImplementationRole.QueryPerformer, [reason], 1, 1, [stub]);
    }

    internal static string ReturnType(SemanticKeyedQuery query, string model) => query switch
    {
        { Cardinality: SemanticQueryCardinality.Many, Delivery: SemanticQueryDelivery.Live } => $"global::System.Reactive.Subjects.ISubject<global::System.Collections.Generic.IEnumerable<{model}>>",
        { Cardinality: SemanticQueryCardinality.Many } => $"global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<{model}>>",
        _ => $"global::System.Threading.Tasks.Task<{model}?>"
    };

    internal static ArtifactRenderDiagnostic? Analyze(SemanticApplicationContext context, SemanticKeyedQuery query, SemanticImplementationRequirement requirement, string body)
    {
        ArtifactRenderDiagnostic Error(string detail) => new(
            "STAGE-IMPL-005",
            ArtifactRenderDiagnosticSeverity.Error,
            $"Query performer requirement '{requirement.RequirementId}' at {PureTransitionAdmission.SourceLocation(requirement, body, 0)}: {detail}",
            query.Id);

        var collision = Arguments(query).Select(Name).FirstOrDefault(name => string.Equals(name, "readModels", StringComparison.Ordinal) || string.Equals(name, "context", StringComparison.Ordinal));
        if (collision is not null) return Error($"Argument '{collision}' conflicts with a reserved query performer body binding.");

        var parsed = PureTransitionAdmission.ParseBody(body, new HashSet<string>(StringComparer.Ordinal), "query performer", enforcePurity: false);
        if (parsed.Rejection is { } rejection) return Error(rejection.Reason!);

        var types = new SemanticTypeSystem(context);
        var model = context.ReadModels[query.ReadModel];
        var modelName = Identifiers.ToPascalCase(model.Name);
        var located = context.DeclaringSlice(model.Id);
        var builder = new CSharpCodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path)).Using(context.CommonNamespace);
        var declaration = SemanticStateViewArtifactRenderer.ModelDeclaration(model, types, [query], context.Reducers.Any(reducer => reducer.ReadModel == model.Id));
        builder.OpenBlock(declaration.Replace("[global::Cratis.Chronicle.Keys.KeyAttribute] ", string.Empty, StringComparison.Ordinal));
        RenderBody(builder, query, modelName, context.MemberNames.Query(query), types, body, requirement.RequirementId);
        builder.EndBlock();
        var code = builder.ToString();
        var options = new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest);
        var tree = CSharpSyntaxTree.ParseText(code, options);
        var declarations = AnalysisDeclarations(context, types);
        MetadataReference[] references;
        try
        {
            references = [.. PureTransitionAdmission.AnalysisReferences(), SemanticGenerationBodyAdmission.FundamentalsReference];
        }
        catch (InvalidOperationException exception)
        {
            return Error(exception.Message);
        }

        var compilation = CSharpCompilation.Create(
            "QueryPerformerAnalysis",
            [CSharpSyntaxTree.ParseText(ImplicitUsings, options), .. declarations.Select(declaration => CSharpSyntaxTree.ParseText(declaration, options)), tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var diagnostics = compilation.GetDiagnostics();
        var error = diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) ??
            diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning && !diagnostic.IsSuppressed);
        if (error is null) return null;

        var location = CSharpInlineIndentation.Locate(code, body);
        var bodyOffset = location is null ? 0 : Math.Clamp(location.ToBody(error.Location.SourceSpan.Start - location.Start), 0, Math.Max(0, body.Length - 1));
        return new(
            "STAGE-IMPL-005",
            ArtifactRenderDiagnosticSeverity.Error,
            $"Query performer requirement '{requirement.RequirementId}' at {PureTransitionAdmission.SourceLocation(requirement, body, bodyOffset)}: {error.Id}: {error.GetMessage(CultureInfo.InvariantCulture)}",
            query.Id);
    }

    static string Parameters(SemanticKeyedQuery query, SemanticTypeSystem types)
    {
        string[] dependencies =
        [
            "global::Cratis.Chronicle.ReadModels.IReadModels __stageReadModelStore",
            "global::Cratis.Arc.Queries.IQueryContextManager __stageQueryContexts",
            "global::Cratis.Arc.Tenancy.ITenantIdAccessor __stageTenantAccessor",
            "global::Cratis.Arc.Authorization.ICurrentPrincipalAccessor __stagePrincipalAccessor"
        ];
        return string.Join(", ", dependencies.Concat(Arguments(query).Select(argument => Parameter(argument, types, optionalDefault: true))));
    }

    static void RenderBody(CSharpCodeBuilder builder, SemanticKeyedQuery query, string model, string method, SemanticTypeSystem types, string body, string requirementId)
    {
        RenderContextDeclarations(builder, query, method, types);
        var parameters = Arguments(query).Select(argument => Parameter(argument, types));
        var result = query.Cardinality == SemanticQueryCardinality.Many ? $"global::System.Collections.Generic.IEnumerable<{model}>" : $"{model}?";

        // The performer lives in a type of its own: a non-public static method on the read model that returns read models is what Arc's analyzer
        // takes for a query that was forgotten to be made public (ARC0029).
        builder.OpenBlock($"static class StagePerformer_{method}")
            .OpenBlock($"public static {result} Perform(global::System.Collections.Generic.IEnumerable<{model}> readModels, StageQueryContext_{method} context{string.Concat(parameters.Select(parameter => $", {parameter}"))})")
            .RawVerbatim(body, requirementId)
            .EndBlock()
            .EndBlock();
    }

    static void RenderContextDeclarations(CSharpCodeBuilder builder, SemanticKeyedQuery query, string method, SemanticTypeSystem types) =>
        builder.Line($"sealed record StageQueryArguments_{method}({string.Join(", ", Arguments(query).Select(argument => $"{types.Type(argument.Type)} {Identifiers.ToPascalCase(argument.Name)}"))});")
            .Line($"sealed record StageQueryContext_{method}(StageQueryArguments_{method} Arguments, string Tenant, global::System.Security.Claims.ClaimsPrincipal Identity, global::System.DateTimeOffset ReceivedAt);")
            .BlankLine();

    static List<string> AnalysisDeclarations(SemanticApplicationContext context, SemanticTypeSystem types)
    {
        // Reuse the actual concept declarations and pinned Fundamentals metadata, without loading
        // Arc or Chronicle. Only the external Chronicle base is a stand-in, as in reducer analysis.
        var declarations = new List<string>
        {
            $"namespace {context.CommonNamespace} {{ internal class StageQueryMarker {{ }} }}",
            "namespace Cratis.Chronicle.Events { public record EventSourceId<T>(T TypedValue) : global::Cratis.Concepts.ConceptAs<T>(TypedValue) where T : global::System.IComparable { public static implicit operator T(EventSourceId<T> value) => value.TypedValue; } }"
        };
        declarations.AddRange(SemanticBodyDeclarations.RealConcepts(context));

        foreach (var readModel in context.ReadModels.Values.Where(SemanticVersionFeatures.HasCompositeKey))
        {
            var ns = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(readModel.Id).Path);
            declarations.Add($"namespace {ns} {{ {SemanticStateViewArtifactRenderer.CompositeKeyRecord(readModel, types)} }}");
        }

        foreach (var composite in context.Application.Types)
        {
            declarations.Add($"namespace {context.CommonNamespace} {{ public record {Identifiers.ToPascalCase(composite.Name)}({Properties(composite.Properties, types)}); }}");
        }

        return declarations;
    }

    static string Properties(IEnumerable<SemanticProperty> properties, SemanticTypeSystem types) =>
        string.Join(", ", properties.Select(property => $"{types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}"));

    static IEnumerable<SemanticReadModelQueryArgument> Arguments(SemanticKeyedQuery query)
    {
        if (query.Argument is { } key) yield return key;
        foreach (var filter in query.Filters) yield return new(filter.Id, filter.Name, filter.Type);
    }

    static string Name(SemanticReadModelQueryArgument argument) => Identifiers.EscapeKeyword(Identifiers.ToCamelCase(argument.Name));

    static string Parameter(SemanticReadModelQueryArgument argument, SemanticTypeSystem types, bool optionalDefault = false) =>
        $"{types.Type(argument.Type)} {Name(argument)}{(optionalDefault && argument.Type.IsOptional ? " = null" : string.Empty)}";
}
