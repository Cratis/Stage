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
        var transition = context.Projections.Values.Single(_ => _.ReadModel == readModel.Id).Transitions.Single();
        var @event = context.Events[transition.EventContract];
        var produced = specification.ThenEvents.Single(_ => _.EventContract == @event.Id);
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

    static string Conditional(string content) => $"#if DEBUG\n{content}\n#endif\n";
}
