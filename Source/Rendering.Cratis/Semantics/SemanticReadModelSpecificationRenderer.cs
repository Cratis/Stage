// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders projected read-model expectations directly from ESM.
/// </summary>
internal static class SemanticReadModelSpecificationRenderer
{
    /// <summary>
    /// Renders one expected read-model state.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="expected">The expected read-model state.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated specification source.</returns>
    public static RenderedFile Render(
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected,
        SemanticApplicationContext context)
    {
        var readModel = context.ReadModels[expected.ReadModel];
        var projection = context.Projections.Values.SingleOrDefault(_ => _.ReadModel == readModel.Id);
        var reducer = context.Reducers.SingleOrDefault(_ => _.ReadModel == readModel.Id);
        var command = specification.When is null ? null : context.Commands[specification.When.Command];

        // Without a command there is nothing to replay: the facts are all given, and only a projection builds the model from them.
        var replay = command is null ? [] : ReplayOf(context.Application, specification, expected, projection, reducer, command);
        var givenEvents = reducer is null ? specification.GivenEvents : SemanticReducerSpecificationEvents.Given(specification, expected, reducer);
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_projected";
        if (specification.ThenReadModels.Length > 1)
        {
            behavior += $"_into_{Identifiers.ToSnakeCase(readModel.Name)}";
        }
        var builder = Builder(behavior, located, readModel, context);
        var readModelName = types.SliceType(readModel.Id, readModel.Name);

        context.Docs(specification.Id).Render(builder, $"Specifies {Identifiers.ToWords(specification.Name)}.")
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line($"readonly global::Cratis.Chronicle.Testing.ReadModels.ReadModelScenario<{readModelName}> _scenario = new();")
            .Line(SemanticSpecificationValuesRenderer.Declarations)
            .BlankLine()
            .OpenBlock("async global::System.Threading.Tasks.Task Establish()");
        foreach (var given in givenEvents)
        {
            var givenEvent = context.Events[given.EventContract];
            var givenArguments = givenEvent.Properties.Select(property =>
                types.Value(given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            var givenSource = given.EventSource!;
            builder.Line($"await _scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(givenSource.Value, givenSource.Type), givenSource.Type)}){SemanticSpecificationRouteRendering.Seed(given.Route, context, types)}.Events(new {types.EventType(givenEvent)}({string.Join(", ", givenArguments)}));");
        }

        foreach (var (produced, expectedEvent) in replay)
        {
            var @event = context.Events[produced.EventContract];
            var source = SemanticDestinations.ForSpecification(specification, command!, produced);
            var arguments = @event.Properties.Select(property =>
                types.Value(expectedEvent.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            builder.Line($"await _scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type)}){SemanticSpecificationRouteRendering.Seed(SemanticSpecificationRouteRendering.ForCommand(specification, context), context, types)}.Events(new {types.EventType(@event)}({string.Join(", ", arguments)}));");
        }

        builder.EndBlock()
            .BlankLine();
        var keyProperty = readModel.Properties.Single(_ => _.IsIdentifier);
        var instance = $"_scenario.InstanceForEventSourceId({types.EventSourceExpression(types.Value(expected.Key, keyProperty.Type), keyProperty.Type)})!";
        if (expected.Values.IsEmpty)
        {
            builder.Line($"[global::Xunit.FactAttribute] void should_project_the_expected_instance() => {instance}.ShouldNotBeNull();");
        }

        foreach (var value in expected.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal))
        {
            var property = readModel.Properties.Single(_ => _.Id == value.TargetProperty);
            if (property.Type.IsCollection && value.Value is SemanticArrayValue array)
            {
                var elementType = property.Type with { IsCollection = false, IsOptional = false };
                var elements = string.Join(", ", array.Values.Select(element => types.Value(element, elementType)));
                var name = Identifiers.ToSnakeCase(property.Name);
                var optionalAccess = reducer is null ? "!" : "!.Value";
                builder.Line($"static readonly {types.Type(elementType)}[] _expected_{name} = [{elements}];")
                    .Line($"[global::Xunit.FactAttribute] void should_project_{name}() => " +
                        $"global::Xunit.Assert.True(global::System.Linq.Enumerable.SequenceEqual({instance}.{Identifiers.ToPascalCase(property.Name)}" +
                        (property.Type.IsOptional ? optionalAccess : string.Empty) + $", _expected_{name}));");
            }
            else
            {
                builder.Line(
                    $"[global::Xunit.FactAttribute] void should_project_{Identifiers.ToSnakeCase(property.Name)}() => " +
                    $"{instance}.{Identifiers.ToPascalCase(property.Name)}.ShouldEqual({types.Value(value.Value, property.Type)});");
            }
        }

        builder.Line("/// <inheritdoc/>").Line("public void Dispose() => _scenario.Dispose();").EndBlock();
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);

        // Decided from the rendered content, as the non-semantic renderer does: only a culture-invariant
        // parse needs the namespace, and emitting it regardless leaves an unused using in every file.
        var content = types.SpecificationContent(builder.ToString());
        if (SpecificationValues.NeedsGlobalization(content))
        {
            content = types.SpecificationContent(builder.Using("System.Globalization").ToString());
        }

        return new(path, content);
    }

    static CSharpCodeBuilder Builder(
        string behavior,
        LocatedSemanticSlice located,
        SemanticReadModel readModel,
        SemanticApplicationContext context)
    {
        return new CSharpCodeBuilder()
            .Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Specifications");
    }

    static IReadOnlyList<(SemanticProducedEvent Produced, SemanticSpecificationEvent Expected)> ReplayOf(
        SemanticApplication application,
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected,
        SemanticProjection? projection,
        SemanticReducer? reducer,
        SemanticCommand command) =>
        reducer is null
            ? SemanticProjectionSpecificationEvents.Replay(application, specification, projection!, command)
            : SemanticReducerSpecificationEvents.Replay(application, specification, expected, reducer, command);
}
