// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders optional snapshot query expectations directly from ESM.
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
        var readModel = context.ReadModels[query.ReadModel];
        var result = expected.Results.Single();
        var projection = context.Projections.Values.Single(_ => _.ReadModel == readModel.Id);
        var produced = projection.Scope is { } scope
            ? specification.ThenEvents.Single(expectedEvent => scope.From.Any(from => from.EventContract == expectedEvent.EventContract))
            : specification.ThenEvents.Single(expectedEvent => expectedEvent.EventContract == projection.Transitions.Single().EventContract);
        var @event = context.Events[produced.EventContract];
        var command = context.Commands[specification.When!.Command];
        var source = SemanticDestinations.ForSpecification(specification, command, command.Produces.First(_ => _.EventContract == @event.Id));
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context);
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_queried";
        if (specification.ThenQueries.Length > 1)
        {
            behavior += $"_through_{Identifiers.ToSnakeCase(query.Name)}";
        }
        var readModelName = Identifiers.ToPascalCase(readModel.Name);
        var queryNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(query.Id).Path);
        var builder = new CSharpCodeBuilder()
            .Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.ReadModels")
            .Using("Cratis.Chronicle.Testing.ReadModels")
            .Using("Cratis.Specifications")
            .Using("NSubstitute")
            .Using("Xunit")
            .Using($"{context.RootNamespace}.Common")
            .Using(queryNamespace);

        var predicates = result.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal).Select(value =>
        {
            var property = readModel.Properties.Single(_ => _.Id == value.TargetProperty);
            return $"_result.{Identifiers.ToPascalCase(property.Name)} == {types.Value(value.Value, property.Type)}";
        });
        var predicate = $"_result is not null{string.Concat(predicates.Select(_ => $" && {_}"))}";
        var key = types.Value(expected.Key, query.Argument.Type);
        var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path);
        if (!string.Equals(eventNamespace, queryNamespace, StringComparison.Ordinal))
        {
            builder.Using(eventNamespace);
        }

        foreach (var given in specification.GivenEvents)
        {
            var givenNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(given.EventContract).Path);
            if (!string.Equals(givenNamespace, queryNamespace, StringComparison.Ordinal))
            {
                builder.Using(givenNamespace);
            }
        }

        builder.OpenBlock($"public class {behavior} : Specification")
            .Line("readonly IReadModels _readModels = Substitute.For<IReadModels>();")
            .Line($"readonly ReadModelScenario<{readModelName}> _scenario = new();")
            .Line($"{readModelName}? _result;")
            .BlankLine()
            .OpenBlock("async Task Establish()");
        foreach (var given in specification.GivenEvents)
        {
            var givenEvent = context.Events[given.EventContract];
            var givenArguments = givenEvent.Properties.Select(property =>
                types.Value(given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            var givenSource = given.EventSource!;
            builder.Line($"await _scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(givenSource.Value, givenSource.Type), givenSource.Type)}).Events(new {Identifiers.ToPascalCase(givenEvent.Name)}({string.Join(", ", givenArguments)}));");
        }

        var eventArguments = @event.Properties.Select(property =>
            types.Value(produced.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
        builder.Line($"await _scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type)}).Events(new {Identifiers.ToPascalCase(@event.Name)}({string.Join(", ", eventArguments)}));")
            .Line($"_readModels.GetInstanceById<{readModelName}>((EventSourceId){key}).Returns(_scenario.InstanceForEventSourceId((EventSourceId){key})!);")
            .EndBlock()
            .BlankLine()
            .Line($"async Task Because() => _result = await {readModelName}.{Identifiers.ToPascalCase(query.Name)}(_readModels, {key});")
            .BlankLine()
            .Line($"[Fact] void should_return_the_expected_read_model() => ({predicate}).ShouldBeTrue();")
            .EndBlock();

        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);

        // Decided from the rendered content, as the non-semantic renderer does: only a culture-invariant
        // parse needs the namespace, and emitting it regardless leaves an unused using in every file.
        var content = builder.ToString();
        if (SpecificationValues.NeedsGlobalization(content))
        {
            content = builder.Using("System.Globalization").ToString();
        }

        return new(path, Conditional(content));
    }

    /// <summary>
    /// Renders a query-only lookup of a complete pre-seeded read model. This uses the exact store
    /// read by the generated query; it does not simulate projection initial state.
    /// </summary>
    /// <param name="specification">The query-only specification.</param>
    /// <param name="expected">The query result to compare.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated query specification source.</returns>
    internal static RenderedFile RenderSeededQuery(
        SemanticSpecification specification,
        SemanticSpecificationQueryResult expected,
        SemanticApplicationContext context)
    {
        var query = context.Queries[expected.Query];
        var readModel = context.ReadModels[query.ReadModel];
        var given = specification.GivenReadModels.Single();
        var result = expected.Results.Single();
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context);
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_queried";
        var readModelName = Identifiers.ToPascalCase(readModel.Name);
        var builder = new CSharpCodeBuilder()
            .Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Testing.ReadModels")
            .Using("Cratis.Specifications")
            .Using("Xunit")
            .Using(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(query.Id).Path));
        var key = types.Value(given.Key, query.Argument.Type);
        var values = readModel.Properties.OrderBy(property => property.Id.ToString(), StringComparer.Ordinal)
            .Select(property => types.Value(given.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
        var predicates = result.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal).Select(value =>
        {
            var property = readModel.Properties.Single(_ => _.Id == value.TargetProperty);
            return $"_result.{Identifiers.ToPascalCase(property.Name)} == {types.Value(value.Value, property.Type)}";
        });
        var predicate = $"_result is not null{string.Concat(predicates.Select(_ => $" && {_}"))}";
        if (SemanticTypeSystem.ValueNeedsCommon(given.Key, query.Argument.Type) ||
            given.Values.Any(value => SemanticTypeSystem.ValueNeedsCommon(value.Value, readModel.Properties.Single(_ => _.Id == value.TargetProperty).Type)) ||
            result.Values.Any(value => SemanticTypeSystem.ValueNeedsCommon(value.Value, readModel.Properties.Single(_ => _.Id == value.TargetProperty).Type)))
        {
            builder.Using($"{context.RootNamespace}.Common");
        }

        builder.OpenBlock($"public class {behavior} : Specification")
            .Line($"readonly ReadModelScenario<{readModelName}> _scenario = new();")
            .Line($"{readModelName}? _result;")
            .BlankLine()
            .Line($"void Establish() => _scenario.Given.ForEventSourceId((EventSourceId){key}).ReadModel(new {readModelName}({string.Join(", ", values)}));")
            .BlankLine()
            .Line($"async Task Because() => _result = await {readModelName}.{Identifiers.ToPascalCase(query.Name)}(_scenario.ReadModels, {key});")
            .BlankLine()
            .Line($"[Fact] void should_return_the_seeded_read_model() => ({predicate}).ShouldBeTrue();")
            .EndBlock();
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);
        var content = builder.ToString();
        if (SpecificationValues.NeedsGlobalization(content))
        {
            content = builder.Using("System.Globalization").ToString();
        }

        return new(path, Conditional(content));
    }

    static string Conditional(string content) => $"#if DEBUG\n{content}\n#endif\n";
}
