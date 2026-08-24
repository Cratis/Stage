// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders command acceptance and rejection specifications directly from ESM.
/// </summary>
internal static class SemanticCommandSpecificationRenderer
{
    /// <summary>
    /// Renders the command outcome of one semantic specification.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated specification source.</returns>
    public static RenderedFile Render(SemanticSpecification specification, SemanticApplicationContext context)
    {
        var command = context.Commands[specification.When.Command];
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context);
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}";
        var ownNamespace = $"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}";
        var builder = new CSharpCodeBuilder()
            .Namespace(ownNamespace)
            .Using("Cratis.Arc.Commands")
            .Using("Cratis.Arc.Testing.Commands")
            .Using("Cratis.Specifications")
            .Using("System.Globalization")
            .Using("Xunit")
            .Using($"{context.RootNamespace}.Common");

        foreach (var expected in specification.ThenEvents)
        {
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(expected.EventContract).Path);
            if (!string.Equals(eventNamespace, SliceNaming.Namespace(context.RootNamespace, located.Path), StringComparison.Ordinal))
            {
                builder.Using(eventNamespace);
            }
        }

        var commandName = Identifiers.ToPascalCase(command.Name);
        var arguments = command.Properties.Select(property =>
            types.Value(specification.When.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
        builder.OpenBlock($"public class {behavior} : Specification")
            .Line($"readonly CommandScenario<{commandName}> _scenario = new();")
            .Line("CommandResult _result = null!;")
            .BlankLine()
            .Line($"async Task Because() => _result = await _scenario.Execute(new {commandName}({string.Join(", ", arguments)}));")
            .BlankLine();

        if (!specification.ThenErrors.IsEmpty)
        {
            builder.Line("[Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();")
                .Line("[Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();");
        }
        else
        {
            RenderAccepted(builder, specification, command, commandName, context, types);
        }

        builder.EndBlock();
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);
        return new(path, Conditional(builder.ToString()));
    }

    static void RenderAccepted(
        CSharpCodeBuilder builder,
        SemanticSpecification specification,
        SemanticCommand command,
        string commandName,
        SemanticApplicationContext context,
        SemanticTypeSystem types)
    {
        var produced = command.Produces.Single();
        var destination = (SemanticResolvedExpression)produced.Destination!;
        var destinationProperty = command.Properties.Single(_ => _.Id == destination.Target);
        var destinationValue = specification.When.Values.Single(_ => _.TargetProperty == destination.Target).Value;
        builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
            .Line("[Fact] void should_succeed() => _result.ShouldBeSuccessful();");

        foreach (var expected in specification.ThenEvents)
        {
            var @event = context.Events[expected.EventContract];
            var predicate = string.Join(" && ", @event.Properties.Select(property =>
            {
                var value = expected.Values.Single(_ => _.TargetProperty == property.Id).Value;
                return $"@event.{Identifiers.ToPascalCase(property.Name)} == {types.Value(value, property.Type)}";
            }));
            builder.Line(
                $"[Fact] async Task should_have_appended_{Identifiers.ToSnakeCase(@event.Name)}() => " +
                $"await _scenario.ShouldHaveAppendedEvent<{commandName}, {Identifiers.ToPascalCase(@event.Name)}>(" +
                $"{types.Value(destinationValue, destinationProperty.Type)}, @event => {predicate});");
        }
    }

    static string Conditional(string content) => $"#if DEBUG\n{content}\n#endif\n";
}
