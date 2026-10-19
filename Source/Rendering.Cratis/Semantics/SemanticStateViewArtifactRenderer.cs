// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
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
                .. located.Slice.Projections.Select(_ => _.Id), .. located.Slice.Queries.Select(_ => _.Id),
                .. located.Slice.Events.Select(_ => _.Id), .. SelfReducerSources(located, context)]
        };
    }

    internal static void Configure(CSharpCodeBuilder builder, LocatedSemanticSlice located, SemanticApplicationContext context) =>
        context.ReducerBindings(located).Configure(builder);

    internal static void ConfigureImports(CSharpCodeBuilder builder, LocatedSemanticSlice located, SemanticApplicationContext context)
    {
        var ownNamespace = SliceNaming.Namespace(context.RootNamespace, located.Path);
        builder.Using("Cratis.Arc.Authorization")
            .Using("Cratis.Arc.Queries.ModelBound")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Projections.ModelBound")
            .Using("Cratis.Chronicle.ReadModels");
        if (context.SelfReducing && !located.Slice.Reducers.IsEmpty)
        {
            builder.Using($"{context.RootNamespace}.TypedContexts");
        }
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

            var reducer = located.Slice.Reducers.SingleOrDefault(candidate => candidate.ReadModel == readModel.Id);
            if (context.SelfReducing && reducer?.From is { } filter)
            {
                SemanticObserverFilterRendering.Attribute(builder, filter, context);
            }
            builder.Attribute("global::Cratis.Arc.Queries.ModelBound.ReadModelAttribute")
                .OpenBlock(transition is null ? ModelDeclaration(readModel, types, queries, reducer is not null) :
                    $"public record {Identifiers.ToPascalCase(readModel.Name)}({Parameters(readModel, transition, @event!, types, queries.FirstOrDefault(query => query.Cardinality == SemanticQueryCardinality.ZeroOrOne))})");
            foreach (var query in queries)
            {
                RenderQuery(builder, query, readModel, types, context);
            }

            if (context.SelfReducing && reducer is not null)
            {
                builder.BlankLine();
                SemanticReducerArtifactRenderer.RenderTransitions(builder, reducer, context);
            }
            builder.EndBlock();
            if (SemanticVersionFeatures.HasCompositeKey(readModel))
            {
                builder.BlankLine().Raw(CompositeKeyRecord(readModel, types));
            }

            if (projection?.Scope is { } scope)
            {
                builder.BlankLine().Raw(SemanticScopedProjectionRenderer.Render(projection, readModel, scope, context));
            }
        }

        // An event a state-view slice declares (the projection removes or reads it, another slice's command may produce it) is generated
        // here: nothing else renders it.
        foreach (var declaredEvent in located.Slice.Events)
        {
            if (!firstModel || located.Slice.Events[0] != declaredEvent)
            {
                builder.BlankLine();
            }

            SemanticStateChangeArtifactRenderer.RenderEvent(builder, declaredEvent, types, context);
        }
    }

    internal static IOrderedEnumerable<SemanticProperty> OrderedProperties(IEnumerable<SemanticProperty> properties) =>
        properties.OrderBy(property => property.Id.ToString(), StringComparer.Ordinal);

    internal static bool QueryNeedsCommon(SemanticKeyedQuery query) =>
        (query.Argument is not null && SemanticTypeSystem.DeclarationNeedsCommon(query.Argument.Type)) ||
        query.Filters.Any(filter => SemanticTypeSystem.DeclarationNeedsCommon(filter.Type));

    internal static IEnumerable<SemanticId> SelfReducerSources(LocatedSemanticSlice located, SemanticApplicationContext context) =>
        context.SelfReducing ? located.Slice.Reducers.SelectMany(reducer => reducer.Transitions.Select(transition => transition.EventContract)).Distinct() : [];

    internal static string ModelDeclaration(SemanticReadModel model, SemanticTypeSystem types, IReadOnlyList<SemanticKeyedQuery> queries, bool reducerInput) =>
        $"public record {Identifiers.ToPascalCase(model.Name)}({ScopedParameters(model, types, queries, reducerInput)})";

    // Chronicle identifies an instance with one object-valued key, so a composite-keyed model carries the key record
    // as its [Key] property next to the declared properties, which keep their own values.
    internal static string CompositeKeyRecord(SemanticReadModel readModel, SemanticTypeSystem types) =>
        $"/// <summary>\n/// Identifies one {Identifiers.ToPascalCase(readModel.Name)} by its key parts.\n/// </summary>\n" +
        string.Join('\n', OrderedProperties(readModel.Properties.Where(property => property.IsIdentifier)).Select(property =>
            $"/// <param name=\"{Identifiers.ToPascalCase(property.Name)}\">The {Identifiers.ToPascalCase(property.Name)} key part.</param>")) +
        $"\npublic record {SemanticCompositeKeyAdmission.KeyTypeName(readModel)}({string.Join(", ", OrderedProperties(readModel.Properties.Where(property => property.IsIdentifier)).Select(property => $"{types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}"))});";

    internal static string ScopedParameters(SemanticReadModel readModel, SemanticTypeSystem types, IReadOnlyList<SemanticKeyedQuery> queries, bool reducerInput) =>
        SemanticVersionFeatures.HasCompositeKey(readModel)
            ? string.Join(", ", OrderedProperties(readModel.Properties).Select(property => $"{types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}")
                .Prepend($"[global::Cratis.Chronicle.Keys.KeyAttribute] {SemanticCompositeKeyAdmission.KeyTypeName(readModel)} {SemanticCompositeKeyAdmission.KeyPropertyName}"))
            : string.Join(", ", OrderedProperties(readModel.Properties).Select(property =>
            $"{((property.IsIdentifier || queries.Any(_ => _.Cardinality == SemanticQueryCardinality.ZeroOrOne && _.KeyProperty == property.Id)) && !types.IsEventSourceIdentifier(property.Type) ? "[global::Cratis.Chronicle.Keys.KeyAttribute] " : string.Empty)}{types.Type(property.Type, reducerInput)} {Identifiers.ToPascalCase(property.Name)}"));

    internal static string Parameters(
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

    internal static void RenderQuery(
        CSharpCodeBuilder builder,
        SemanticKeyedQuery query,
        SemanticReadModel readModel,
        SemanticTypeSystem types,
        SemanticApplicationContext context)
    {
        var readModelName = Identifiers.ToPascalCase(readModel.Name);
        var methodName = context.MemberNames.Query(query);
        var documentation = context.Docs(query.Id).Render(builder.BlankLine())
            .Attribute(SemanticAuthorizationAttributes.For(query, context));
        if (methodName != Identifiers.ToPascalCase(query.Name))
        {
            var path = GeneratedApiRoutes.QueryPath(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(query.Id).Path), Identifiers.ToPascalCase(query.Name));
            documentation.Attribute($"global::Cratis.Arc.Queries.ModelBound.PathAttribute({CSharpCodeBuilder.StringLiteral(path)})");
        }
        if (query is { Cardinality: SemanticQueryCardinality.Many, Delivery: SemanticQueryDelivery.Live } || !query.Filters.IsDefaultOrEmpty)
        {
            RenderCollectionQuery(documentation, query, readModel, types, context);
            return;
        }

        if (query.Argument is null)
        {
            documentation.ExpressionMember(
                $"public static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<{readModelName}>> {methodName}(global::Cratis.Chronicle.ReadModels.IReadModels readModels)",
                $"await readModels.GetInstances<{readModelName}>()");
            return;
        }

        var argument = QueryArgument.For(query.Argument, types);
        if (query.Cardinality == SemanticQueryCardinality.Many)
        {
            var property = readModel.Properties.Single(_ => _.Id == query.KeyProperty);
            documentation.ExpressionMember(
                $"public static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<{readModelName}>> {methodName}(global::Cratis.Chronicle.ReadModels.IReadModels readModels, {argument.Parameter})",
                $"global::System.Linq.Enumerable.Where(await readModels.GetInstances<{readModelName}>(), instance => global::System.Collections.Generic.EqualityComparer<{types.Type(query.Argument.Type)}>.Default.Equals(instance.{Identifiers.ToPascalCase(property.Name)}, {argument.Name}))");
            return;
        }

        // Arc's analyzers flag a raw string or Guid parameter that the method converts to an event source id, so declare the id itself.
        var raw = query.Argument.Type.Kind == SemanticTypeReferenceKind.Primitive && query.Argument.Type.Primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid;
        documentation.ExpressionMember(
            $"public static async global::System.Threading.Tasks.Task<{readModelName}?> {methodName}(global::Cratis.Chronicle.ReadModels.IReadModels readModels, {(raw ? $"global::Cratis.Chronicle.Events.EventSourceId {argument.Name}" : argument.Parameter)})",
            $"await readModels.GetInstanceById<{readModelName}>({(raw ? argument.Name : $"(global::Cratis.Chronicle.Events.EventSourceId){argument.Name}")})");
    }

    static void RenderCollectionQuery(CSharpCodeBuilder builder, SemanticKeyedQuery query, SemanticReadModel readModel, SemanticTypeSystem types, SemanticApplicationContext context)
    {
        var observable = query is { Cardinality: SemanticQueryCardinality.Many, Delivery: SemanticQueryDelivery.Live };
        var filters = query.Filters.Select(filter => RenderFilter(filter, readModel, types, observable)).ToArray();
        var parameters = KeyParameters(query, types).Concat(filters.Select(filter => filter.Parameter))
            .Prepend("global::Cratis.Chronicle.ReadModels.IReadModels readModels");
        var predicates = KeyPredicates(query, readModel, types).Concat(filters.Select(filter => filter.Predicate)).ToArray();
        var readModelName = Identifiers.ToPascalCase(readModel.Name);
        var methodName = context.MemberNames.Query(query);
        var filterExpression = predicates.Length == 0 ? string.Empty : $"instance => {string.Join(" && ", predicates)}";
        var expression = observable
            ? $"readModels.Observe<{readModelName}>({filterExpression})"
            : $"global::System.Linq.Enumerable.Where(await readModels.GetInstances<{readModelName}>(), {filterExpression})";
        var returnType = observable
            ? $"global::System.Reactive.Subjects.ISubject<global::System.Collections.Generic.IEnumerable<{readModelName}>>"
            : $"async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<{readModelName}>>";
        builder.ExpressionMember(
            $"public static {returnType} {methodName}({string.Join(", ", parameters)})",
            GuardRequiredFilters(expression, filters, readModelName, methodName));
    }

    static IEnumerable<string> KeyParameters(SemanticKeyedQuery query, SemanticTypeSystem types) =>
        query.Argument is { } keyed ? [QueryArgument.For(keyed, types).Parameter] : [];

    static IEnumerable<string> KeyPredicates(SemanticKeyedQuery query, SemanticReadModel readModel, SemanticTypeSystem types) =>
        query.Argument is { } keyed
            ? [EqualityPredicate(new(keyed.Type, readModel.Properties.Single(property => property.Id == query.KeyProperty), QueryArgument.For(keyed, types)), types)]
            : [];

    static string EqualityPredicate(QueryEquality equality, SemanticTypeSystem types) =>
        $"global::System.Collections.Generic.EqualityComparer<{types.Type(equality.Type with { IsOptional = equality.Type.IsOptional || equality.Property.Type.IsOptional })}>.Default.Equals(instance.{Identifiers.ToPascalCase(equality.Property.Name)}, {equality.Argument.Name})";

    static RenderedQueryFilter RenderFilter(SemanticQueryFilter filter, SemanticReadModel readModel, SemanticTypeSystem types, bool expressionTree = false)
    {
        var argument = QueryArgument.For(new(filter.Id, filter.Name, filter.Type), types);
        var property = readModel.Properties.Single(property => property.Id == filter.Property);
        var equality = EqualityPredicate(new(filter.Type, property, argument), types);

        // Arc treats plain reference parameters as implicitly optional. Match its required-by
        // rejection explicitly for text.
        return new(
            argument.Parameter + (filter.Type.IsOptional ? " = null" : string.Empty),
            filter.Type.IsOptional ? $"({argument.Name} {(expressionTree ? "== null" : "is null")} || {equality})" : equality,
            !filter.Type.IsOptional && argument.Type == "string" ? argument : null);
    }

    static string GuardRequiredFilters(string expression, IEnumerable<RenderedQueryFilter> filters, string readModelName, string methodName)
    {
        foreach (var guard in filters.Select(filter => filter.RequiredGuard).OfType<QueryArgument>().Reverse())
        {
            expression = $"{guard.Name} is null ? throw new global::Cratis.Arc.Queries.ModelBound.MissingArgumentForQuery(nameof({guard.Name}), typeof({guard.Type}), $\"{{typeof({readModelName}).FullName}}.{methodName}\") : {expression}";
        }

        return expression;
    }

    sealed record RenderedQueryFilter(string Parameter, string Predicate, QueryArgument? RequiredGuard);

    sealed record QueryEquality(SemanticTypeReference Type, SemanticProperty Property, QueryArgument Argument);

    sealed record QueryArgument(string Name, string Type)
    {
        public string Parameter => $"{Type} {Name}";

        public static QueryArgument For(SemanticReadModelQueryArgument argument, SemanticTypeSystem types) =>
            new(Identifiers.EscapeKeyword(Identifiers.ToCamelCase(argument.Name)), types.Type(argument.Type));
    }
}
