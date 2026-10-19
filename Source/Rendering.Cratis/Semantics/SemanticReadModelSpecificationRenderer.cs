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
        => Render(specification, expected, context, null);

    /// <summary>
    /// Renders the absence of one keyed read-model instance.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="expected">The expected absent instance.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="index">The expectation's position, to keep multiple absent keys distinct.</param>
    /// <returns>The generated specification source.</returns>
    public static RenderedFile RenderAbsent(
        SemanticSpecification specification,
        SemanticSpecificationAbsentReadModel expected,
        SemanticApplicationContext context,
        int index) => Render(specification, new(expected.ReadModel, expected.Key, []), context, index);

    static RenderedFile Render(
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected,
        SemanticApplicationContext context,
        int? absentIndex)
    {
        var readModel = context.ReadModels[expected.ReadModel];
        var projection = context.Projections.Values.SingleOrDefault(_ => _.ReadModel == readModel.Id);
        var reducer = context.Reducers.SingleOrDefault(_ => _.ReadModel == readModel.Id);
        var command = specification.When is null ? null : context.Commands[specification.When.Command];

        // Without a command there is nothing to replay: the facts are all given, and only a projection builds the model from them.
        IReadOnlyList<(SemanticProducedEvent Produced, SemanticSpecificationEvent Expected)> replay = [];
        if (command is not null)
        {
            replay = absentIndex is not null ? ReplayForAbsence(context, specification, command)
                : ReplayOf(context.Application, specification, expected, projection, reducer, command);
        }
        var givenEvents = reducer is null ? specification.GivenEvents : SemanticReducerSpecificationEvents.Given(specification, expected, reducer);
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_projected";
        if (specification.ThenReadModels.Length > 1)
        {
            behavior += $"_into_{Identifiers.ToSnakeCase(readModel.Name)}";
        }
        if (absentIndex is { } index)
        {
            behavior += $"_without_{Identifiers.ToSnakeCase(readModel.Name)}_{index + 1}";
        }
        var builder = Builder(behavior, located, readModel, context);
        var needsCommon = false;
        var readModelName = types.SliceType(readModel.Id, readModel.Name);

        context.Docs(specification.Id).Render(builder)
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line($"readonly global::Cratis.Chronicle.Testing.ReadModels.ReadModelScenario<{readModelName}> _scenario = new();")
            .Line(SemanticSpecificationValuesRenderer.Declarations)
            .BlankLine()
            .OpenBlock(givenEvents.Count == 0 && replay.Count == 0 ? "void Establish()" : "async global::System.Threading.Tasks.Task Establish()");
        foreach (var given in givenEvents)
        {
            var givenEvent = context.Events[given.EventContract];
            var givenNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(given.EventContract).Path);
            if (!string.Equals(givenNamespace, SliceNaming.Namespace(context.RootNamespace, located.Path), StringComparison.Ordinal))
            {
                builder.Using(givenNamespace);
            }

            var givenArguments = givenEvent.Properties.Select(property =>
                types.Value(given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            var givenSource = given.EventSource!;
            needsCommon |= SemanticTypeSystem.ValueNeedsCommon(givenSource.Value, givenSource.Type) ||
                givenEvent.Properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(
                    given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            builder.Line($"await _scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(givenSource.Value, givenSource.Type), givenSource.Type)}){SemanticSpecificationRouteRendering.Seed(given.Route, context, types)}.Events(new {types.EventType(givenEvent)}({string.Join(", ", givenArguments)}));");
        }

        foreach (var (produced, expectedEvent) in replay)
        {
            var @event = context.Events[produced.EventContract];
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path);
            if (!string.Equals(eventNamespace, SliceNaming.Namespace(context.RootNamespace, located.Path), StringComparison.Ordinal))
            {
                builder.Using(eventNamespace);
            }

            var source = SemanticDestinations.ForSpecification(specification, command!, produced);
            needsCommon |= SemanticTypeSystem.ValueNeedsCommon(source.Value, source.Type) ||
                @event.Properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(
                    expectedEvent.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            var arguments = @event.Properties.Select(property =>
                types.Value(expectedEvent.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            builder.Line($"await _scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type)}){SemanticSpecificationRouteRendering.Seed(SemanticSpecificationRouteRendering.ForCommand(specification, context), context, types)}.Events(new {types.EventType(@event)}({string.Join(", ", arguments)}));");
        }

        builder.EndBlock()
            .BlankLine();
        var keyProperty = readModel.Properties.Single(_ => _.IsIdentifier);
        needsCommon |= SemanticTypeSystem.ValueNeedsCommon(expected.Key, keyProperty.Type);
        var instance = $"_scenario.InstanceForEventSourceId({types.EventSourceExpression(types.Value(expected.Key, keyProperty.Type), keyProperty.Type)})!";
        if (absentIndex is not null)
        {
            var key = types.EventSourceExpression(types.Value(expected.Key, keyProperty.Type), keyProperty.Type);
            builder.Line($"[global::Xunit.FactAttribute] void should_not_project_the_absent_instance() => _scenario.Instances.ContainsKey({key}).ShouldBeFalse();");
        }
        else if (expected.Values.IsEmpty)
        {
            builder.Line($"[global::Xunit.FactAttribute] void should_project_the_expected_instance() => {instance}.ShouldNotBeNull();");
        }

        foreach (var value in expected.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal))
        {
            var property = readModel.Properties.Single(_ => _.Id == value.TargetProperty);
            if (property.Type.IsCollection && value.Value is SemanticArrayValue array)
            {
                var elementType = property.Type with { IsCollection = false, IsOptional = false };
                needsCommon |= SemanticTypeSystem.DeclarationNeedsCommon(elementType) ||
                    SemanticTypeSystem.ValueNeedsCommon(value.Value, property.Type);
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
                needsCommon |= SemanticTypeSystem.ValueNeedsCommon(value.Value, property.Type);
                builder.Line(
                    $"[global::Xunit.FactAttribute] void should_project_{Identifiers.ToSnakeCase(property.Name)}() => " +
                    $"{instance}.{Identifiers.ToPascalCase(property.Name)}.ShouldEqual({types.Value(value.Value, property.Type)});");
            }
        }

        builder.Line("public void Dispose() => _scenario.Dispose();").EndBlock();
        if (needsCommon)
        {
            builder.Using(context.CommonNamespace);
        }
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
            .Using("Cratis.Chronicle.Testing.ReadModels")
            .Using("Cratis.Specifications")
            .Using("Xunit")
            .Using(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(readModel.Id).Path));
    }

    static IReadOnlyList<(SemanticProducedEvent Produced, SemanticSpecificationEvent Expected)> ReplayForAbsence(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        SemanticCommand command) =>
        specification.ThenNoEvents || specification.ThenDenied || !specification.ThenErrors.IsEmpty ? [] :
            [.. SemanticProductionSelection.Taken(context.Application, command, specification).Select((produced, index) => (produced,
                specification.ThenEventsInAnyOrder
                    ? specification.ThenEvents.Single(expected => expected.EventContract == produced.EventContract)
                    : specification.ThenEvents[index]))];

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
