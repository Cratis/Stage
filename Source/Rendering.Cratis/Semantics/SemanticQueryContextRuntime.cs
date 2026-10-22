// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
        var parameters = new List<string>
        {
            "global::Cratis.Chronicle.ReadModels.IReadModels readModelStore",
            "global::Cratis.Arc.Queries.IQueryContextManager queryContexts",
            "global::Cratis.Arc.Tenancy.ITenantIdAccessor tenantAccessor",
            "global::Cratis.Arc.Authorization.ICurrentPrincipalAccessor principalAccessor"
        };
        parameters.AddRange(arguments.Select(argument => Parameter(argument, types, optionalDefault: true)));
        var live = query.Delivery == SemanticQueryDelivery.Live;
        var invocation = $"StagePerform_{method}(readModels, context{string.Concat(arguments.Select(argument => $", {Name(argument)}"))})";
        builder.OpenBlock($"public static {(live ? string.Empty : "async ")}{returnType} {method}({string.Join(", ", parameters)})")
            .Line($"var context = new StageQueryContext_{method}(new StageQueryArguments_{method}({string.Join(", ", arguments.Select(Name))}), tenantAccessor.Current.Value, queryContexts.Current.AuthorizedPrincipal ?? principalAccessor.Current, queryContexts.Current.ReceivedAt);");
        if (live)
        {
            builder.Line($"var source = readModelStore.Observe<{model}>();")
                .Line($"return global::System.Reactive.Subjects.Subject.Create<global::System.Collections.Generic.IEnumerable<{model}>>(source, global::System.Reactive.Linq.Observable.Select(source, readModels => {invocation}));");
        }
        else
        {
            builder.Line($"var readModels = await readModelStore.GetInstances<{model}>();")
                .Line($"return {invocation};");
        }

        builder.EndBlock().BlankLine();
        RenderBody(builder, query, model, method, types, requirement, body);
    }

    internal static ArtifactRenderDiagnostic? Analyze(SemanticApplicationContext context, SemanticKeyedQuery query, SemanticImplementationRequirement requirement, string body)
    {
        ArtifactRenderDiagnostic Error(string detail) => new(
            "STAGE-IMPL-005",
            ArtifactRenderDiagnosticSeverity.Error,
            $"Query performer requirement '{requirement.RequirementId}' at {PureTransitionAdmission.SourceLocation(requirement, body, 0)}: {detail}",
            query.Id);

        var parsed = PureTransitionAdmission.ParseBody(body, new HashSet<string>(StringComparer.Ordinal), "query performer", enforcePurity: false);
        if (parsed.Rejection is { } rejection) return Error(rejection.Reason!);

        var types = new SemanticTypeSystem(context);
        var model = context.ReadModels[query.ReadModel];
        var modelName = Identifiers.ToPascalCase(model.Name);
        var located = context.DeclaringSlice(model.Id);
        var builder = new CSharpCodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path)).Using(context.CommonNamespace);
        var declaration = SemanticStateViewArtifactRenderer.ModelDeclaration(model, types, [query], context.Reducers.Any(reducer => reducer.ReadModel == model.Id));
        builder.OpenBlock(declaration.Replace("[global::Cratis.Chronicle.Keys.KeyAttribute] ", string.Empty, StringComparison.Ordinal));
        RenderBody(builder, query, modelName, context.MemberNames.Query(query), types, requirement, body);
        builder.EndBlock();
        var code = builder.ToString();
        var options = new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest);
        var tree = CSharpSyntaxTree.ParseText(code, options);
        var declarations = AnalysisDeclarations(context, types);
        MetadataReference[] references;
        try
        {
            references = PureTransitionAdmission.AnalysisReferences();
        }
        catch (InvalidOperationException exception)
        {
            return Error(exception.Message);
        }

        var compilation = CSharpCompilation.Create(
            "QueryPerformerAnalysis",
            [CSharpSyntaxTree.ParseText(ImplicitUsings + declarations, options), tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var diagnostics = compilation.GetDiagnostics();
        var error = diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) ??
            diagnostics.FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning && !diagnostic.IsSuppressed);
        if (error is null) return null;

        var offset = code.IndexOf(body, StringComparison.Ordinal);
        var bodyOffset = Math.Clamp(error.Location.SourceSpan.Start - offset, 0, Math.Max(0, body.Length - 1));
        return new(
            "STAGE-IMPL-005",
            ArtifactRenderDiagnosticSeverity.Error,
            $"Query performer requirement '{requirement.RequirementId}' at {PureTransitionAdmission.SourceLocation(requirement, body, bodyOffset)}: {error.Id}: {error.GetMessage(CultureInfo.InvariantCulture)}",
            query.Id);
    }

    static void RenderBody(CSharpCodeBuilder builder, SemanticKeyedQuery query, string model, string method, SemanticTypeSystem types, SemanticImplementationRequirement requirement, string body)
    {
        var arguments = Arguments(query).ToArray();
        builder.Line($"sealed record StageQueryArguments_{method}({string.Join(", ", arguments.Select(argument => $"{types.Type(argument.Type)} {Identifiers.ToPascalCase(argument.Name)}"))});")
            .Line($"sealed record StageQueryContext_{method}(StageQueryArguments_{method} Arguments, string Tenant, global::System.Security.Claims.ClaimsPrincipal Identity, global::System.DateTimeOffset ReceivedAt);")
            .BlankLine();
        var parameters = arguments.Select(argument => Parameter(argument, types));
        var result = query.Cardinality == SemanticQueryCardinality.Many ? $"global::System.Collections.Generic.IEnumerable<{model}>" : $"{model}?";
        builder.OpenBlock($"static {result} StagePerform_{method}(global::System.Collections.Generic.IEnumerable<{model}> readModels, StageQueryContext_{method} context{string.Concat(parameters.Select(parameter => $", {parameter}"))})");
        var line = requirement.File is null && !requirement.BodyLines.IsDefaultOrEmpty ? requirement.BodyLines[0].Line : 1;
        var source = requirement.File ?? requirement.Source.Span.Document.ToString();
        builder.Line($"#line {line} {CSharpCodeBuilder.StringLiteral(source)}")
            .RawVerbatim(body)
            .Line("#line default")
            .EndBlock();
    }

    static string AnalysisDeclarations(SemanticApplicationContext context, SemanticTypeSystem types)
    {
        // Boundary types are BCL-only here, just as in the generated body helper. Do not load Arc or
        // Chronicle into the renderer process; the generated-app specs bind the real boundary.
        var declarations = new List<string> { $"namespace {context.CommonNamespace} {{ internal class StageQueryMarker {{ }} }}" };
        foreach (var concept in context.Application.Concepts)
        {
            var name = Identifiers.ToPascalCase(concept.Name);
            if (!concept.Values.IsEmpty)
            {
                declarations.Add($"namespace {context.CommonNamespace} {{ public enum {name} {{ {string.Join(", ", SemanticEnumMembers.Declarations(concept))} }} }}");
                continue;
            }

            var scalar = types.Primitive(concept.Primitive);
            declarations.Add($"namespace {context.CommonNamespace} {{ public record {name}({scalar} Value) {{ {(context.IdentifierConcepts.Contains(concept.Id) ? $"public {scalar} TypedValue => Value;" : string.Empty)} public static implicit operator {scalar}({name} value) => value.Value; public static implicit operator {name}({scalar} value) => new(value); }} }}");
        }

        foreach (var readModel in context.ReadModels.Values.Where(SemanticVersionFeatures.HasCompositeKey))
        {
            var ns = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(readModel.Id).Path);
            declarations.Add($"namespace {ns} {{ {SemanticStateViewArtifactRenderer.CompositeKeyRecord(readModel, types)} }}");
        }

        foreach (var composite in context.Application.Types)
        {
            declarations.Add($"namespace {context.CommonNamespace} {{ public record {Identifiers.ToPascalCase(composite.Name)}({Properties(composite.Properties, types)}); }}");
        }

        return string.Join('\n', declarations);
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
