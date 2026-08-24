// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

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
        var transition = projection.Transitions.Single();
        var @event = context.Events[transition.EventContract];
        var expectedEvent = specification.ThenEvents.Single(_ => _.EventContract == @event.Id);
        var command = context.Commands[specification.When.Command];
        var destination = (SemanticResolvedExpression)command.Produces.Single(_ => _.EventContract == @event.Id).Destination!;
        var destinationProperty = command.Properties.Single(_ => _.Id == destination.Target);
        var destinationValue = specification.When.Values.Single(_ => _.TargetProperty == destination.Target).Value;
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context);
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_projected";
        var builder = Builder(behavior, located, readModel, @event, context);
        var readModelName = Identifiers.ToPascalCase(readModel.Name);
        var eventArguments = @event.Properties.Select(property =>
            types.Value(expectedEvent.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));

        builder.OpenBlock($"public class {behavior} : Specification")
            .Line($"readonly ReadModelScenario<{readModelName}> _scenario = new();")
            .BlankLine()
            .Line("async Task Establish() => await _scenario.Given")
            .Line($"    .ForEventSource({types.Value(destinationValue, destinationProperty.Type)})")
            .Line($"    .Events(new {Identifiers.ToPascalCase(@event.Name)}({string.Join(", ", eventArguments)}));")
            .BlankLine();
        foreach (var property in readModel.Properties)
        {
            var value = expected.Values.Single(_ => _.TargetProperty == property.Id).Value;
            builder.Line(
                $"[Fact] void should_project_{Identifiers.ToSnakeCase(property.Name)}() => " +
                $"_scenario.Instance!.{Identifiers.ToPascalCase(property.Name)}.ShouldEqual({types.Value(value, property.Type)});");
        }

        builder.EndBlock();
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);
        return new(path, Conditional(builder.ToString()));
    }

    static CSharpCodeBuilder Builder(
        string behavior,
        LocatedSemanticSlice located,
        SemanticReadModel readModel,
        SemanticEventContract @event,
        SemanticApplicationContext context)
    {
        var builder = new CSharpCodeBuilder()
            .Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Chronicle.Testing.ReadModels")
            .Using("Cratis.Specifications")
            .Using("System.Globalization")
            .Using("Xunit")
            .Using($"{context.RootNamespace}.Common")
            .Using(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(readModel.Id).Path));
        var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path);
        if (!string.Equals(eventNamespace, SliceNaming.Namespace(context.RootNamespace, located.Path), StringComparison.Ordinal))
        {
            builder.Using(eventNamespace);
        }

        return builder;
    }

    static string Conditional(string content) => $"#if DEBUG\n{content}\n#endif\n";
}
