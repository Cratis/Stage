// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Specifications;

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
        var when = specification.When!;
        var command = context.Commands[when.Command];
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context);
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}";
        var ownNamespace = $"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}";
        var builder = new CSharpCodeBuilder()
            .Namespace(ownNamespace)
            .Using("Cratis.Arc.Commands")
            .Using("Cratis.Arc.Testing.Commands")
            .Using("Cratis.Specifications")
            .Using("Xunit");
        if (command.Properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(
            when.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type)))
        {
            builder.Using($"{context.RootNamespace}.Common");
        }

        if (!specification.GivenEvents.IsEmpty)
        {
            builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
                .Using("Cratis.Chronicle.Events");
        }

        foreach (var expected in specification.GivenEvents.Concat(specification.ThenEvents))
        {
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(expected.EventContract).Path);
            if (!string.Equals(eventNamespace, SliceNaming.Namespace(context.RootNamespace, located.Path), StringComparison.Ordinal))
            {
                builder.Using(eventNamespace);
            }
        }

        var commandName = Identifiers.ToPascalCase(command.Name);
        var arguments = command.Properties.Select(property =>
            types.Value(when.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));

        // The scenario owns a service provider and disposes it, so the specification that owns the scenario
        // has to dispose it in turn. Generated code is built in someone else's repository, frequently with
        // analysis as errors, and an undisposed owned resource is a build failure there that they cannot fix
        // by editing the file.
        builder.OpenBlock($"public class {behavior} : Specification, IDisposable")
            .Line($"readonly CommandScenario<{commandName}> _scenario = new();")
            .Line("CommandResult _result = null!;")
            .BlankLine();

        if (!specification.GivenEvents.IsEmpty)
        {
            builder.OpenBlock("void Establish()");
        }

        foreach (var given in specification.GivenEvents)
        {
            var @event = context.Events[given.EventContract];
            var source = given.EventSource!;
            if (SemanticTypeSystem.ValueNeedsCommon(source.Value, source.Type) ||
                @event.Properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(
                    given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type)))
            {
                builder.Using($"{context.RootNamespace}.Common");
            }

            var eventArguments = @event.Properties.Select(property =>
                types.Value(given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            builder.Line($"_scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type)}).Events(new {Identifiers.ToPascalCase(@event.Name)}({string.Join(", ", eventArguments)}));");
        }

        if (!specification.GivenEvents.IsEmpty)
        {
            builder.EndBlock().BlankLine();
        }

        builder.Line($"async Task Because() => _result = await _scenario.Execute(new {commandName}({string.Join(", ", arguments)}));")
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

        builder.BlankLine()
            .ExpressionMember("public void Dispose()", "_scenario.Dispose()")
            .EndBlock();
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);

        // Decided from the rendered content rather than predicted, the same way the non-semantic renderer
        // does it: only a value that renders as a culture-invariant parse needs the namespace, and emitting
        // it regardless leaves a using nobody uses in every generated specification.
        var content = builder.ToString();
        if (SpecificationValues.NeedsGlobalization(content))
        {
            content = builder.Using("System.Globalization").ToString();
        }

        return new(path, Conditional(content));
    }

    static void RenderAccepted(
        CSharpCodeBuilder builder,
        SemanticSpecification specification,
        SemanticCommand command,
        string commandName,
        SemanticApplicationContext context,
        SemanticTypeSystem types)
    {
        var source = SemanticDestinations.ForSpecification(specification, command, command.Produces.Single());
        builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
            .Using("Cratis.Chronicle.Events")
            .Line("[Fact] void should_succeed() => _result.ShouldBeSuccessful();");

        foreach (var expected in specification.ThenEvents)
        {
            var @event = context.Events[expected.EventContract];
            if (SemanticTypeSystem.ValueNeedsCommon(source.Value, source.Type) ||
                @event.Properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(
                    expected.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type)))
            {
                builder.Using($"{context.RootNamespace}.Common");
            }

            var predicate = string.Join(" && ", @event.Properties.Select(property =>
            {
                var value = expected.Values.Single(_ => _.TargetProperty == property.Id).Value;
                return $"@event.{Identifiers.ToPascalCase(property.Name)} == {types.Value(value, property.Type)}";
            }));
            builder.Line(
                $"[Fact] async Task should_have_appended_{Identifiers.ToSnakeCase(@event.Name)}() => " +
                $"await _scenario.ShouldHaveAppendedEvent<{commandName}, {Identifiers.ToPascalCase(@event.Name)}>(" +
                $"{types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type)}, @event => {predicate});");
        }
    }

    static string Conditional(string content) => $"#if DEBUG\n{content}\n#endif\n";
}
