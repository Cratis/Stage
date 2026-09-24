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
        var projection = context.Projections.Values.Single(_ => _.ReadModel == readModel.Id);
        var command = context.Commands[specification.When!.Command];
        var replay = SemanticProjectionSpecificationEvents.Replay(specification, projection, command);
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context);
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_projected";
        if (specification.ThenReadModels.Length > 1)
        {
            behavior += $"_into_{Identifiers.ToSnakeCase(readModel.Name)}";
        }
        var builder = Builder(behavior, located, readModel, context);
        var readModelName = Identifiers.ToPascalCase(readModel.Name);

        builder.OpenBlock($"public class {behavior} : Specification")
            .Line($"readonly ReadModelScenario<{readModelName}> _scenario = new();")
            .BlankLine()
            .OpenBlock("async Task Establish()");
        foreach (var given in specification.GivenEvents)
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
            builder.Line($"await _scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(givenSource.Value, givenSource.Type), givenSource.Type)}).Events(new {Identifiers.ToPascalCase(givenEvent.Name)}({string.Join(", ", givenArguments)}));");
        }

        foreach (var (produced, expectedEvent) in replay)
        {
            var @event = context.Events[produced.EventContract];
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path);
            if (!string.Equals(eventNamespace, SliceNaming.Namespace(context.RootNamespace, located.Path), StringComparison.Ordinal))
            {
                builder.Using(eventNamespace);
            }

            var source = SemanticDestinations.ForSpecification(specification, command, produced);
            var arguments = @event.Properties.Select(property =>
                types.Value(expectedEvent.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            builder.Line($"await _scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type)}).Events(new {Identifiers.ToPascalCase(@event.Name)}({string.Join(", ", arguments)}));");
        }

        builder.EndBlock()
            .BlankLine();
        var keyProperty = readModel.Properties.Single(_ => _.IsIdentifier);
        var instance = $"_scenario.InstanceForEventSourceId({types.EventSourceExpression(types.Value(expected.Key, keyProperty.Type), keyProperty.Type)})!";
        if (expected.Values.IsEmpty)
        {
            builder.Line($"[Fact] void should_project_the_expected_instance() => {instance}.ShouldNotBeNull();");
        }

        foreach (var value in expected.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal))
        {
            var property = readModel.Properties.Single(_ => _.Id == value.TargetProperty);
            builder.Line(
                $"[Fact] void should_project_{Identifiers.ToSnakeCase(property.Name)}() => " +
                $"{instance}.{Identifiers.ToPascalCase(property.Name)}.ShouldEqual({types.Value(value.Value, property.Type)});");
        }

        builder.EndBlock();
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
            .Using($"{context.RootNamespace}.Common")
            .Using(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(readModel.Id).Path));
    }

    static string Conditional(string content) => $"#if DEBUG\n{content}\n#endif\n";
}
