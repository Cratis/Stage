// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders snapshot and observable query expectations directly from ESM.
/// </summary>
internal static class SemanticQuerySpecificationRenderer
{
    /// <summary>
    /// Renders one expected query result.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="expected">The expected query result.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated specification source.</returns>
    public static RenderedFile Render(
        SemanticSpecification specification,
        SemanticSpecificationQueryResult expected,
        SemanticApplicationContext context)
    {
        var query = context.Queries[expected.Query];
        if (query.Authorization is not null)
        {
            return SemanticProtectedQuerySpecificationRenderer.Render(specification, expected, context);
        }

        var readModel = context.ReadModels[query.ReadModel];
        var projection = context.Projections.Values.Single(_ => _.ReadModel == readModel.Id);
        var command = specification.When is null ? null : context.Commands[specification.When.Command];
        var replay = command is null ? [] : SemanticProjectionSpecificationEvents.Replay(context.Application, specification, projection, command);
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_queried";
        if (specification.ThenQueries.Length > 1)
        {
            behavior += $"_through_{Identifiers.ToSnakeCase(query.Name)}";
        }
        var readModelName = types.SliceType(readModel.Id, readModel.Name);
        var queryNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(query.Id).Path);
        var builder = context.SpecificationBuilder()
            .Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Specifications")
            .Using("Xunit")
            .Using(context.CommonNamespace)
            .Using(queryNamespace);
        if (!IsLiveList(query))
        {
            builder.Using("Cratis.Chronicle.ReadModels")
                .Using("Cratis.Chronicle.Testing.ReadModels")
                .Using("NSubstitute");
        }

        var key = query.Argument is null ? string.Empty : types.Value(expected.Key, query.Argument.Type);
        var collectionCall = CollectionArguments(query, expected, types);
        foreach (var (produced, _) in replay)
        {
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(produced.EventContract).Path);
            if (!string.Equals(eventNamespace, queryNamespace, StringComparison.Ordinal))
            {
                builder.Using(eventNamespace);
            }
        }

        foreach (var given in specification.GivenEvents)
        {
            var givenNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(given.EventContract).Path);
            if (!string.Equals(givenNamespace, queryNamespace, StringComparison.Ordinal))
            {
                builder.Using(givenNamespace);
            }
        }

        context.Docs(specification.Id).Render(builder)
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable");
        RenderScenario(builder, query, readModelName);
        builder.Line(SemanticSpecificationValuesRenderer.Declarations).BlankLine().OpenBlock("async global::System.Threading.Tasks.Task Establish()");
        foreach (var given in specification.GivenEvents)
        {
            var givenEvent = context.Events[given.EventContract];
            var givenArguments = givenEvent.Properties.Select(property =>
                types.Value(given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            builder.Line(SeedEvent(query, SemanticSpecificationEventSources.Expression(context, types, given), $"new {types.EventType(givenEvent)}({string.Join(", ", givenArguments)})", given.Route, context, types));
        }

        foreach (var (produced, expectedEvent) in replay)
        {
            var @event = context.Events[produced.EventContract];
            var source = SemanticDestinations.ForSpecification(specification, command!, produced);
            var arguments = @event.Properties.Select(property =>
                types.Value(expectedEvent.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            builder.Line(SeedEvent(query, types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type), $"new {types.EventType(@event)}({string.Join(", ", arguments)})", SemanticSpecificationRouteRendering.ForCommand(specification, context, produced), context, types));
        }

        if (query.Cardinality == SemanticQueryCardinality.Many)
        {
            RenderCollectionExpectation(builder, query, expected, readModel, types, collectionCall, context);
        }
        else
        {
            builder.Line($"_readModels.GetInstanceById<{readModelName}>((global::Cratis.Chronicle.Events.EventSourceId){key}).Returns(_scenario.InstanceForEventSourceId((global::Cratis.Chronicle.Events.EventSourceId){key})!);")
                .EndBlock()
                .BlankLine()
                .Line($"async global::System.Threading.Tasks.Task Because() => _result = await {readModelName}.{context.MemberNames.Query(query)}(_readModels, {key});")
                .BlankLine()
                .Line("[global::Xunit.FactAttribute] void should_return_the_expected_read_model() => (_result is not null).ShouldBeTrue();");
            RenderProperties(builder, expected.Results.Single().Values, readModel, types, context);
            builder.Line("public void Dispose() => _scenario.Dispose();").EndBlock();
        }

        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);

        // Decided from the rendered content, as the non-semantic renderer does: only a culture-invariant
        // parse needs the namespace, and emitting it regardless leaves an unused using in every file.
        var content = types.SpecificationContent(builder);
        if (SpecificationValues.NeedsGlobalization(content))
        {
            content = types.SpecificationContent(builder.Using("System.Globalization"));
        }

        return new(path, content);
    }

    /// <summary>
    /// Renders a query-only lookup of a complete pre-seeded read model. This uses the exact store
    /// read by the generated query; it does not simulate projection initial state.
    /// </summary>
    /// <param name="specification">The query-only specification.</param>
    /// <param name="expected">The query result to compare.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated query specification source.</returns>
    /// <exception cref="UnsupportedSemanticRendering">Thrown when the query does not declare the keyed argument required by this renderer.</exception>
    internal static RenderedFile RenderSeededQuery(
        SemanticSpecification specification,
        SemanticSpecificationQueryResult expected,
        SemanticApplicationContext context)
    {
        var query = context.Queries[expected.Query];
        var readModel = context.ReadModels[query.ReadModel];
        var argument = query.Argument ?? throw UnsupportedSemanticRendering.For("seeded query argument", query.Name);
        var given = specification.GivenReadModels.Single();
        var result = expected.Results.Single();
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_queried";
        var readModelName = types.SliceType(readModel.Id, readModel.Name);
        var builder = context.SpecificationBuilder()
            .Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Testing.ReadModels")
            .Using("Cratis.Specifications")
            .Using("Xunit")
            .Using(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(query.Id).Path));
        var key = types.Value(given.Key, argument.Type);
        var values = readModel.Properties.OrderBy(property => property.Id.ToString(), StringComparer.Ordinal)
            .Select(property => types.Value(given.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
        if (SemanticTypeSystem.ValueNeedsCommon(given.Key, argument.Type) ||
            given.Values.Any(value => SemanticTypeSystem.ValueNeedsCommon(value.Value, readModel.Properties.Single(_ => _.Id == value.TargetProperty).Type)) ||
            result.Values.Any(value => SemanticTypeSystem.ValueNeedsCommon(value.Value, readModel.Properties.Single(_ => _.Id == value.TargetProperty).Type)))
        {
            builder.Using(context.CommonNamespace);
        }

        context.Docs(specification.Id).Render(builder)
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line($"readonly global::Cratis.Chronicle.Testing.ReadModels.ReadModelScenario<{readModelName}> _scenario = new();")
            .Line(SemanticSpecificationValuesRenderer.Declarations)
            .Line($"{readModelName}? _result;")
            .BlankLine()
            .Line($"void Establish() => _scenario.Given.ForEventSourceId((global::Cratis.Chronicle.Events.EventSourceId){key}).ReadModel(new {readModelName}({string.Join(", ", values)}));")
            .BlankLine()
            .Line($"async global::System.Threading.Tasks.Task Because() => _result = await {readModelName}.{context.MemberNames.Query(query)}(_scenario.ReadModels, {key});")
            .BlankLine()
            .Line("[global::Xunit.FactAttribute] void should_return_the_seeded_read_model() => (_result is not null).ShouldBeTrue();");
        RenderProperties(builder, result.Values, readModel, types, context);
        builder.Line("public void Dispose() => _scenario.Dispose();").EndBlock();
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);
        var content = types.SpecificationContent(builder);
        if (SpecificationValues.NeedsGlobalization(content))
        {
            content = types.SpecificationContent(builder.Using("System.Globalization"));
        }

        return new(path, content);
    }

    static void RenderScenario(CSharpCodeBuilder builder, SemanticKeyedQuery query, string readModelName)
    {
        builder.Line(IsLiveList(query)
                ? "global::Cratis.Chronicle.ReadModels.IReadModels _readModels => _scenario.ReadModels;"
                : "readonly global::Cratis.Chronicle.ReadModels.IReadModels _readModels = global::NSubstitute.Substitute.For<global::Cratis.Chronicle.ReadModels.IReadModels>();")
            .Line(IsLiveList(query)
                ? "readonly global::Cratis.Chronicle.Testing.Events.EventStoreForTesting _scenario = new();"
                : $"readonly global::Cratis.Chronicle.Testing.ReadModels.ReadModelScenario<{readModelName}> _scenario = new();")

            // Only the field the query's cardinality assigns: an unused one is a CS0169 warning in the generated build.
            .Line(query.Cardinality == SemanticQueryCardinality.Many
                ? $"global::System.Collections.Generic.IEnumerable<{readModelName}> _results = [];"
                : $"{readModelName}? _result;");
    }

    static void RenderCollectionExpectation(CSharpCodeBuilder builder, SemanticKeyedQuery query, SemanticSpecificationQueryResult expected, SemanticReadModel readModel, SemanticTypeSystem types, string arguments, SemanticApplicationContext context)
    {
        var readModelName = types.SliceType(readModel.Id, readModel.Name);
        if (!IsLiveList(query))
        {
            builder.Line($"_readModels.GetInstances<{readModelName}>().Returns(_scenario.Instances.Values);");
        }
        builder.EndBlock().BlankLine()
            .Line($"async global::System.Threading.Tasks.Task Because() => _results = await {CollectionExecution(query, readModelName, arguments, context)};")
            .BlankLine();
        if (!query.Filters.IsDefaultOrEmpty)
        {
            builder.Line($"[global::Xunit.FactAttribute] void should_return_the_expected_count() => global::System.Linq.Enumerable.Count(_results).ShouldEqual({expected.Results.Length});");
        }
        var key = readModel.Properties.Single(property => property.IsIdentifier);
        foreach (var (result, index) in expected.Results.Select((result, index) => (result, index)))
        {
            var selection = $"global::System.Linq.Enumerable.Single(_results, result => result.{context.MemberNames.Property(key)} == {types.Value(result.Key, key.Type)})";
            builder.Line($"[global::Xunit.FactAttribute] void should_return_result_{index + 1}() => ({selection} is not null).ShouldBeTrue();");
            foreach (var value in result.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal))
            {
                var property = readModel.Properties.Single(property => property.Id == value.TargetProperty);
                builder.Line($"[global::Xunit.FactAttribute] void should_return_result_{index + 1}_with_{Identifiers.ToSnakeCase(property.Name)}() => {selection}.{context.MemberNames.Property(property)}.ShouldEqual({types.Value(value.Value, property.Type)});");
            }
        }
        builder.Line("public void Dispose() => _scenario.Dispose();").EndBlock();
    }

    static bool IsLiveList(SemanticKeyedQuery query) =>
        query is { Cardinality: SemanticQueryCardinality.Many, Delivery: SemanticQueryDelivery.Live };

    static string SeedEvent(SemanticKeyedQuery query, string source, string @event, SemanticFixtureRoute? route, SemanticApplicationContext context, SemanticTypeSystem types) =>
        IsLiveList(query)
            ? $"(await _scenario.EventLog.Append({source}, {@event}{(route is null ? string.Empty : $", {SemanticSpecificationRouteRendering.Arguments(route, context, types)}")})).IsSuccess.ShouldBeTrue();"
            : $"await _scenario.Given.ForEventSource({source}){SemanticSpecificationRouteRendering.Seed(route, context, types)}.Events({@event});";

    static string CollectionExecution(SemanticKeyedQuery query, string readModelName, string arguments, SemanticApplicationContext context)
    {
        var invocation = $"{readModelName}.{context.MemberNames.Query(query)}({arguments})";
        return query.Delivery == SemanticQueryDelivery.Live
            ? $"global::System.Reactive.Threading.Tasks.TaskObservableExtensions.ToTask(global::System.Reactive.Linq.Observable.FirstAsync({invocation}))"
            : invocation;
    }

    static string CollectionArguments(SemanticKeyedQuery query, SemanticSpecificationQueryResult expected, SemanticTypeSystem types) =>
        string.Join(", ", (query.Argument is null ? Enumerable.Empty<string>() : [types.Value(expected.Key, query.Argument.Type)])
            .Concat(query.Filters.Select(filter => FilterArgument(filter, expected, types))).Prepend("_readModels"));

    static string FilterArgument(SemanticQueryFilter filter, SemanticSpecificationQueryResult expected, SemanticTypeSystem types)
    {
        var value = expected.Filters.SingleOrDefault(value => value.Filter == filter.Id)?.Value ?? SemanticValue.Null;

        return $"{Identifiers.EscapeKeyword(Identifiers.ToCamelCase(filter.Name))}: {types.Value(value, filter.Type)}";
    }

    static void RenderProperties(CSharpCodeBuilder builder, IEnumerable<SemanticPropertyValue> values, SemanticReadModel readModel, SemanticTypeSystem types, SemanticApplicationContext context)
    {
        foreach (var value in values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal))
        {
            var property = readModel.Properties.Single(property => property.Id == value.TargetProperty);
            builder.Line($"[global::Xunit.FactAttribute] void should_have_{Identifiers.ToSnakeCase(property.Name)}() => _result!.{context.MemberNames.Property(property)}.ShouldEqual({types.Value(value.Value, property.Type)});");
        }
    }
}
