// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the admitted state-view ESM capability as a Cratis model-bound read model.
/// </summary>
internal static class SemanticStateViewArtifactRenderer
{
    /// <summary>
    /// Renders one state-view slice.
    /// </summary>
    /// <param name="located">The located semantic slice.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated slice source.</returns>
    public static RenderedFile Render(LocatedSemanticSlice located, SemanticApplicationContext context)
    {
        var builder = new CSharpCodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        Configure(builder, located, context);
        RenderMembers(builder, located, context);
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), SliceNaming.FileName(located.Slice.Name)]);
        return new(path, builder.ToString())
        {
            Sources = [located.Slice.Id, .. located.Slice.ReadModels.Select(_ => _.Id),
                .. located.Slice.Projections.Select(_ => _.Id), .. located.Slice.Queries.Select(_ => _.Id)]
        };
    }

    internal static void Configure(CSharpCodeBuilder builder, LocatedSemanticSlice located, SemanticApplicationContext context)
    {
        var ownNamespace = SliceNaming.Namespace(context.RootNamespace, located.Path);
        builder.Using("Cratis.Arc.Authorization")
            .Using("Cratis.Arc.Queries.ModelBound")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Projections.ModelBound")
            .Using("Cratis.Chronicle.ReadModels");
        if (located.Slice.ReadModels.SelectMany(_ => _.Properties).Any(_ => SemanticTypeSystem.DeclarationNeedsCommon(_.Type)) ||
            located.Slice.Queries.Any(QueryNeedsCommon))
        {
            builder.Using(context.CommonNamespace);
        }

        foreach (var projection in located.Slice.Projections)
        {
            foreach (var eventId in ProjectionReferencedEventNamesAreUnique.Contracts(projection))
            {
                var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(eventId).Path);
                if (!string.Equals(ownNamespace, eventNamespace, StringComparison.Ordinal))
                {
                    builder.Using(eventNamespace);
                }
            }
        }

        if (located.Slice.Queries.Length > 0 || located.Slice.Projections.Any(_ => _.Scope is not null))
        {
            builder.Using("Cratis.Chronicle.Keys");
        }
    }

    internal static void RenderMembers(CSharpCodeBuilder builder, LocatedSemanticSlice located, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var firstModel = true;
        foreach (var readModel in located.Slice.ReadModels)
        {
            if (!firstModel)
            {
                builder.BlankLine();
            }

            firstModel = false;
            var projection = context.Projections.Values.SingleOrDefault(_ => _.ReadModel == readModel.Id);
            var transition = projection?.Scope is null && projection is not null ? projection.Transitions.Single() : null;
            var @event = transition is null ? null : context.Events[transition.EventContract];
            var queries = located.Slice.Queries.Where(_ => _.ReadModel == readModel.Id).ToArray();
            context.Docs(readModel.Id).Render(builder);
            if (@event is not null)
            {
                builder.Attribute($"global::Cratis.Chronicle.Projections.ModelBound.FromEventAttribute<{types.EventType(@event)}>");
            }

            builder.Attribute("global::Cratis.Arc.Queries.ModelBound.ReadModelAttribute")
                .OpenBlock($"public record {Identifiers.ToPascalCase(readModel.Name)}({(transition is null ? ScopedParameters(readModel, types, queries, context.Reducers.Any(reducer => reducer.ReadModel == readModel.Id)) : Parameters(readModel, transition, @event!, types, queries.FirstOrDefault(query => query.Cardinality == SemanticQueryCardinality.ZeroOrOne)))})");
            foreach (var query in queries)
            {
                RenderQuery(builder, query, readModel, types, context);
            }

            builder.EndBlock();
            if (projection?.Scope is { } scope)
            {
                builder.BlankLine().Raw(SemanticScopedProjectionRenderer.Render(projection, readModel, scope, context));
            }
        }
    }

    internal static IOrderedEnumerable<SemanticProperty> OrderedProperties(IEnumerable<SemanticProperty> properties) =>
        properties.OrderBy(property => property.Id.ToString(), StringComparer.Ordinal);

    static bool QueryNeedsCommon(SemanticKeyedQuery query) =>
        (query.Argument is not null && SemanticTypeSystem.DeclarationNeedsCommon(query.Argument.Type)) ||
        query.Filters.Any(filter => SemanticTypeSystem.DeclarationNeedsCommon(filter.Type));

    static string ScopedParameters(SemanticReadModel readModel, SemanticTypeSystem types, IReadOnlyList<SemanticKeyedQuery> queries, bool reducerInput) =>
        string.Join(", ", OrderedProperties(readModel.Properties).Select(property =>
            $"{((property.IsIdentifier || queries.Any(_ => _.Cardinality == SemanticQueryCardinality.ZeroOrOne && _.KeyProperty == property.Id)) && !types.IsEventSourceIdentifier(property.Type) ? "[global::Cratis.Chronicle.Keys.KeyAttribute] " : string.Empty)}{types.Type(property.Type, reducerInput)} {Identifiers.ToPascalCase(property.Name)}"));

    static string Parameters(
        SemanticReadModel readModel,
        SemanticProjectionTransition transition,
        SemanticEventContract @event,
        SemanticTypeSystem types,
        SemanticKeyedQuery? keyedQuery) =>
        string.Join(", ", OrderedProperties(readModel.Properties).Select(property =>
        {
            var mapping = transition.Mappings.Single(_ => _.TargetProperty == property.Id);
            var source = (SemanticResolvedExpression)mapping.Source;
            var eventProperty = @event.Properties.Single(_ => _.Id == source.Target);
            var targetName = Identifiers.ToPascalCase(property.Name);
            var sourceName = Identifiers.ToPascalCase(eventProperty.Name);
            var attribute = string.Equals(targetName, sourceName, StringComparison.Ordinal)
                ? string.Empty
                : $"[global::Cratis.Chronicle.Projections.ModelBound.SetFromAttribute<{types.EventType(@event)}>(nameof({types.EventType(@event)}.{sourceName}))] ";
            var key = keyedQuery?.KeyProperty == property.Id && !types.IsEventSourceIdentifier(property.Type) ? "[global::Cratis.Chronicle.Keys.KeyAttribute] " : string.Empty;
            return $"{key}{attribute}{types.Type(property.Type)} {targetName}";
        }));

    static void RenderQuery(
        CSharpCodeBuilder builder,
        SemanticKeyedQuery query,
        SemanticReadModel readModel,
        SemanticTypeSystem types,
        SemanticApplicationContext context)
    {
        var readModelName = Identifiers.ToPascalCase(readModel.Name);
        var methodName = Identifiers.ToPascalCase(query.Name);
        var documentation = context.Docs(query.Id).Render(builder.BlankLine())
            .Attribute(SemanticAuthorizationAttributes.For(query, context));
        if (!query.Filters.IsDefaultOrEmpty)
        {
            RenderFilteredQuery(documentation, query, readModel, types);
            return;
        }

        if (query.Argument is null)
        {
            documentation.ExpressionMember(
                $"public static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<{readModelName}>> {methodName}(global::Cratis.Chronicle.ReadModels.IReadModels readModels)",
                $"await readModels.GetInstances<{readModelName}>()");
            return;
        }

        var argument = QueryArgumentExpression.For(query.Argument, types);
        if (query.Cardinality == SemanticQueryCardinality.Many)
        {
            var property = readModel.Properties.Single(_ => _.Id == query.KeyProperty);
            documentation.ExpressionMember(
                $"public static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<{readModelName}>> {methodName}(global::Cratis.Chronicle.ReadModels.IReadModels readModels, {argument.Parameter})",
                $"global::System.Linq.Enumerable.Where(await readModels.GetInstances<{readModelName}>(), instance => global::System.Collections.Generic.EqualityComparer<{types.Type(query.Argument.Type)}>.Default.Equals(instance.{Identifiers.ToPascalCase(property.Name)}, {argument.Comparison}))");
            return;
        }

        documentation.ExpressionMember(
            $"public static async global::System.Threading.Tasks.Task<{readModelName}?> {methodName}(global::Cratis.Chronicle.ReadModels.IReadModels readModels, {argument.Parameter})",
            argument.Guarded($"await readModels.GetInstanceById<{readModelName}>((global::Cratis.Chronicle.Events.EventSourceId){argument.Value})"));
    }

    static void RenderFilteredQuery(CSharpCodeBuilder builder, SemanticKeyedQuery query, SemanticReadModel readModel, SemanticTypeSystem types)
    {
        var filters = query.Filters.Select(filter => RenderFilter(filter, readModel, types)).ToArray();
        var parameters = KeyParameters(query, types).Concat(filters.Select(filter => filter.Parameter))
            .Prepend("global::Cratis.Chronicle.ReadModels.IReadModels readModels");
        var predicates = KeyPredicates(query, readModel, types).Concat(filters.Select(filter => filter.Predicate));
        var readModelName = Identifiers.ToPascalCase(readModel.Name);
        var methodName = Identifiers.ToPascalCase(query.Name);
        var expression = $"global::System.Linq.Enumerable.Where(await readModels.GetInstances<{readModelName}>(), instance => {string.Join(" && ", predicates)})";
        builder.ExpressionMember(
            $"public static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<{readModelName}>> {methodName}({string.Join(", ", parameters)})",
            GuardRequiredFilters(expression, filters, readModelName, methodName));
    }

    static IEnumerable<string> KeyParameters(SemanticKeyedQuery query, SemanticTypeSystem types) =>
        query.Argument is { } keyed ? [QueryArgumentExpression.For(keyed, types).Parameter] : [];

    static IEnumerable<string> KeyPredicates(SemanticKeyedQuery query, SemanticReadModel readModel, SemanticTypeSystem types) =>
        query.Argument is { } keyed
            ? [EqualityPredicate(keyed.Type, readModel.Properties.Single(property => property.Id == query.KeyProperty), QueryArgumentExpression.For(keyed, types), types)]
            : [];

    static string EqualityPredicate(SemanticTypeReference type, SemanticProperty property, QueryArgumentExpression argument, SemanticTypeSystem types) =>
        $"global::System.Collections.Generic.EqualityComparer<{types.Type(type with { IsOptional = type.IsOptional || property.Type.IsOptional })}>.Default.Equals(instance.{Identifiers.ToPascalCase(property.Name)}, {argument.Comparison})";

    static RenderedQueryFilter RenderFilter(SemanticQueryFilter filter, SemanticReadModel readModel, SemanticTypeSystem types)
    {
        var argument = QueryArgumentExpression.For(new(filter.Id, filter.Name, filter.Type), types);
        var property = readModel.Properties.Single(property => property.Id == filter.Property);
        var equality = EqualityPredicate(filter.Type, property, argument, types);

        // Arc treats plain reference parameters as implicitly optional. Match its required-by
        // rejection explicitly for text, including validated concepts accepted as text primitives.
        return new(
            argument.Parameter + (filter.Type.IsOptional ? " = null" : string.Empty),
            filter.Type.IsOptional ? $"({argument.Name} is null || {equality})" : equality,
            !filter.Type.IsOptional && argument.Type == "string" ? argument : null);
    }

    static string GuardRequiredFilters(string expression, IEnumerable<RenderedQueryFilter> filters, string readModelName, string methodName)
    {
        foreach (var guard in filters.Select(filter => filter.RequiredGuard).OfType<QueryArgumentExpression>().Reverse())
        {
            expression = $"{guard.Name} is null ? throw new global::Cratis.Arc.Queries.ModelBound.MissingArgumentForQuery(nameof({guard.Name}), typeof({guard.Type}), $\"{{typeof({readModelName}).FullName}}.{methodName}\") : {expression}";
        }

        return expression;
    }

    sealed record RenderedQueryFilter(string Parameter, string Predicate, QueryArgumentExpression? RequiredGuard);

    /// <summary>
    /// The C# expressions for a query argument. A validated concept argument is accepted as its primitive and converted
    /// inside the method, so Arc runs no concept validator on it and an out-of-rule key answers like the reference.
    /// </summary>
    /// <param name="Name">The C# parameter name.</param>
    /// <param name="Type">The C# parameter type.</param>
    /// <param name="Value">The expression that yields the argument as its declared type.</param>
    /// <param name="NullGuarded">Whether an absent optional argument must short-circuit before conversion.</param>
    sealed record QueryArgumentExpression(string Name, string Type, string Value, bool NullGuarded)
    {
        public string Parameter => $"{Type} {Name}";

        public string Comparison => NullGuarded ? $"({Name} is null ? null : {Value})" : Value;

        public static QueryArgumentExpression For(SemanticReadModelQueryArgument argument, SemanticTypeSystem types)
        {
            var name = Identifiers.EscapeKeyword(Identifiers.ToCamelCase(argument.Name));
            var concept = types.ValidatedQueryConcept(argument.Type);
            if (concept is null) return new(name, types.Type(argument.Type), name, false);

            var primitive = argument.Type.IsOptional && concept.Primitive != SemanticPrimitiveType.Text ? $"{name}.Value" : name;
            return new(name, types.QueryArgumentType(argument.Type), $"new {types.CommonType(concept.Name)}({primitive})", argument.Type.IsOptional);
        }

        public string Guarded(string expression) => NullGuarded ? $"{Name} is null ? null : {expression}" : expression;
    }
}
