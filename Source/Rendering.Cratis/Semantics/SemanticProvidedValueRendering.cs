// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits and renders the values a model provides to its request context (Cratis/Stage#1116): the generated application registers
/// one Choreography context value catalog and a scoped resolver that resolves each value lazily, once per request.
/// </summary>
internal static class SemanticProvidedValueRendering
{
    /// <summary>The diagnostic for a provided value whose source cannot render.</summary>
    internal const string UnsupportedSource = "STAGE-ESM-075";

    /// <summary>The diagnostic for a provided value whose type cannot render.</summary>
    internal const string UnsupportedType = "STAGE-ESM-076";

    const string Choreography = "global::Cratis.Choreography.Availability.Context";

    /// <summary>
    /// Refuses every provided value the generated resolver cannot resolve faithfully.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <param name="slices">The selected slices.</param>
    /// <param name="diagnostics">The blocking diagnostics.</param>
    internal static void Admit(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var declaration in SemanticProvidedValues.Declared(context))
        {
            var provided = declaration.Value;
            if (TypeRefusal(context, provided) is { } type)
            {
                diagnostics.Add(Error(UnsupportedType, $"Provided value '{provided.Name}' {type}.", context));
                continue;
            }

            if (SourceRefusal(context, slices, provided) is { } source)
            {
                diagnostics.Add(Error(UnsupportedSource, $"Provided value '{provided.Name}' {source}.", context));
            }
        }
    }

    /// <summary>
    /// Renders the selection-independent resolver and its registration, or nothing when the model provides no value.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <returns>The generated file, or <see langword="null"/>.</returns>
    internal static RenderedFile? Render(SemanticApplicationContext context)
    {
        var declarations = SemanticProvidedValues.Declared(context);
        if (declarations.Count == 0) return null;
        var types = new SemanticTypeSystem(context);
        var queries = declarations.Select(declaration => declaration.Value.Source).OfType<SemanticQueryProvideSource>()
            .Select(source => context.Queries[source.Query]).DistinctBy(query => query.Id).OrderBy(query => query.Name, StringComparer.Ordinal).ToArray();
        var builder = context.CodeBuilder().Namespace($"{context.RootNamespace}.GeneratedProvides")
            .Summary("Registers the resolver of the values the Screenplay application provides to its request context.")
            .OpenBlock("public static partial class Registration")
            .OpenBlock("static partial void RegisterGenerated(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)")
            .Line($"global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<{Choreography}.Providers.IHostContextValues, {Choreography}.Providers.InMemoryHostContextValues>(services);")
            .Line("global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<ProvidedValueResolvers>(services);")
            .EndBlock()
            .EndBlock().BlankLine()
            .Summary("Resolves provided values lazily and once per request, nearest declaration first.")
            .OpenBlock("public sealed class ProvidedValueResolvers(global::System.IServiceProvider services)")
            .Summary("Creates the resolver of one request.")
            .Line("/// <param name=\"scope\">The module, feature and slice names the request runs in, outermost first.</param>")
            .Line("/// <param name=\"ambient\">The values the request supplies by name, such as the command or the query arguments.</param>")
            .Line("/// <returns>The resolver.</returns>")
            .OpenBlock($"public {Choreography}.ContextValueResolver Create(global::System.Collections.Generic.IReadOnlyList<string> scope, global::System.Collections.Generic.IReadOnlyDictionary<string, object?> ambient)")
            .Line($"var catalog = new {Choreography}.ContextValueCatalog();");
        foreach (var declaration in declarations)
        {
            var provided = declaration.Value;
            var required = !(provided.Optional || provided.Type?.IsOptional == true);
            var scope = declaration.Scope.Count == 0 ? $"{Choreography}.ContextScope.Application" : $"{Choreography}.ContextScope.Of({string.Join(", ", declaration.Scope.Select(CSharpCodeBuilder.StringLiteral))})";
            builder.Line($"catalog.Declare(new {Choreography}.ContextValueDeclaration({CSharpCodeBuilder.StringLiteral(provided.Name)}, {Provider(provided, types, context)}, {scope}, {(required ? "true" : "false")}));");
        }

        builder.Line("return catalog.CreateResolver(new " + Choreography + ".ContextScope(scope), ambient);")
            .EndBlock()
            .EndBlock();
        if (queries.Length > 0) RenderQuery(builder, queries, context);
        return new(Path.Combine("GeneratedProvides", "ProvidedValues.cs"), types.SpecificationContent(builder));
    }

    static string Provider(SemanticProvidedValue provided, SemanticTypeSystem types, SemanticApplicationContext context) => provided.Source switch
    {
        SemanticHostProvideSource => $"new {Choreography}.Providers.HostContextValueProvider(global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{Choreography}.Providers.IHostContextValues>(services), {CSharpCodeBuilder.StringLiteral(provided.Name)})",
        SemanticValueProvideSource { Value.Path: { } path } => $"new {Choreography}.Providers.ExpressionContextValueProvider({CSharpCodeBuilder.StringLiteral(path)})",
        SemanticValueProvideSource { Value.Value: { } value } => $"new {Choreography}.Providers.LiteralContextValueProvider({Literal(value, provided, types)})",
        SemanticQueryProvideSource query => $"new {Choreography}.Providers.ReadModelContextValueProvider(new ProvidedReadModelQuery(services), {CSharpCodeBuilder.StringLiteral(context.Queries[query.Query].Name)}, {CSharpCodeBuilder.StringLiteral(query.By[0].Path!)})",
        _ => throw UnsupportedSemanticRendering.For("provided value source", provided.Source.GetType().Name)
    };

    static string Literal(SemanticValue value, SemanticProvidedValue provided, SemanticTypeSystem types) => value switch
    {
        SemanticNullValue => "null",
        _ => $"({types.Primitive(provided.Type!.Primitive)}){types.Value(value, provided.Type!)}"
    };

    static void RenderQuery(CSharpCodeBuilder builder, SemanticKeyedQuery[] queries, SemanticApplicationContext context)
    {
        builder.BlankLine().Summary("Runs the keyed snapshot queries a provided value reads.")
            .OpenBlock($"internal sealed class ProvidedReadModelQuery(global::System.IServiceProvider services) : {Choreography}.Providers.IReadModelQuery")
            .OpenBlock("public async global::System.Threading.Tasks.ValueTask<object?> Execute(string query, object key, global::System.Threading.CancellationToken cancellationToken)")
            .Line("var readModels = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Cratis.Chronicle.ReadModels.IReadModels>(services);")
            .Line("var id = (global::Cratis.Chronicle.Events.EventSourceId)(global::System.Convert.ToString(key, global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);")
            .OpenBlock("switch (query)");
        foreach (var query in queries)
        {
            var located = context.DeclaringSlice(query.Id);
            var readModel = Identifiers.ToPascalCase(context.ReadModels[query.ReadModel].Name);
            builder.Line($"case {CSharpCodeBuilder.StringLiteral(query.Name)}: return await global::{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{readModel}.{context.MemberNames.Query(query)}(readModels, id);");
        }

        builder.Line("default: throw new global::System.InvalidOperationException($\"The provided query '{query}' is not generated.\");")
            .EndBlock().EndBlock().EndBlock();
    }

    static string? TypeRefusal(SemanticApplicationContext context, SemanticProvidedValue provided)
    {
        if (provided.Collection || provided.Type?.IsCollection == true) return "is a collection, which a request-scoped value cannot be yet";
        if (provided.ReadModel is { } readModel) return context.ReadModels.ContainsKey(readModel) ? null : "names a read model that is not generated";
        return provided.Type is { Kind: SemanticTypeReferenceKind.Primitive, Primitive: SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text or SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean }
            ? null
            : "has a type other than a read model or a plain Uuid, String, number or Boolean";
    }

    static string? SourceRefusal(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, SemanticProvidedValue provided)
    {
        switch (provided.Source)
        {
            case SemanticHostProvideSource:
                return null;
            case SemanticValueProvideSource { Value: { Path: null, Value: SemanticArrayValue or SemanticCompositeValue } }:
                return "takes a literal that is not a scalar";
            case SemanticValueProvideSource when provided.ReadModel is not null:
                return "takes a literal or $context value for a read model type";
            case SemanticValueProvideSource:
                return null;
            case SemanticQueryProvideSource query:
                return QueryRefusal(context, slices, provided, query);
            default:
                return "takes its value from a source Stage does not render";
        }
    }

    static string? QueryRefusal(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, SemanticProvidedValue provided, SemanticQueryProvideSource source)
    {
        if (!context.Queries.TryGetValue(source.Query, out var query)) return "reads a query that is not generated";
        if (provided.ReadModel != query.ReadModel) return "reads a query whose result is not the declared read model";
        if (source.By is not { Length: 1 } || source.By[0].Path is null) return "must key its query by exactly one $context value";
        if (query is not { Cardinality: SemanticQueryCardinality.ZeroOrOne, Delivery: SemanticQueryDelivery.Snapshot, Argument: { } argument } ||
            argument.Type is not { Kind: SemanticTypeReferenceKind.Primitive, Primitive: SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text, IsCollection: false })
        {
            return "must read a keyed snapshot query by a Uuid or String key";
        }

        if (query.Authorization is not null || query.Narrowing is not null) return "reads a protected query; an in-process read would bypass its authorization";
        if (!query.Filters.IsDefaultOrEmpty || !query.KeyParts.IsDefaultOrEmpty || context.PerformedQueries.Contains(query.Id)) return "reads a query with filters, composite keys or a performer";
        var located = context.DeclaringSlice(query.Id);
        if (context.DeclaringSlice(query.ReadModel).Slice.Id != located.Slice.Id) return "reads a query declared apart from its read model";
        return slices.Any(selected => selected.Slice.Id == located.Slice.Id) ? null : "reads a query of a slice this render does not select";
    }

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticApplicationContext context) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, context.Application.Id);
}
