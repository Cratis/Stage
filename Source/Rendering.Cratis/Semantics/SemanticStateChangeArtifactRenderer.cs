// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the admitted state-change ESM capability as Cratis model-bound source.
/// </summary>
internal static class SemanticStateChangeArtifactRenderer
{
    /// <summary>
    /// Renders one state-change slice.
    /// </summary>
    /// <param name="located">The located semantic slice.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated slice source.</returns>
    public static RenderedFile Render(LocatedSemanticSlice located, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var command = located.Slice.Commands.Single();
        var produced = command.Produces.Single();
        var @event = context.Events[produced.EventContract];
        var ownNamespace = SliceNaming.Namespace(context.RootNamespace, located.Path);
        var builder = new CSharpCodeBuilder()
            .Namespace(ownNamespace)
            .Using("Cratis.Arc.Authorization")
            .Using("Cratis.Arc.Commands.ModelBound")
            .Using("Cratis.Arc.Validation")
            .Using("Cratis.Chronicle.Events")
            .Using($"{context.RootNamespace}.Common");

        var eventSlice = context.DeclaringSlice(@event.Id);
        var eventNamespace = SliceNaming.Namespace(context.RootNamespace, eventSlice.Path);
        if (!string.Equals(ownNamespace, eventNamespace, StringComparison.Ordinal))
        {
            builder.Using(eventNamespace);
        }

        RenderCommand(builder, command, produced, @event, types);
        foreach (var declaredEvent in located.Slice.Events)
        {
            RenderEvent(builder, declaredEvent, types);
        }

        RenderValidator(builder, command);
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), SliceNaming.FileName(located.Slice.Name)]);
        return new(path, builder.ToString());
    }

    static void RenderCommand(
        CSharpCodeBuilder builder,
        SemanticCommand command,
        SemanticProducedEvent produced,
        SemanticEventContract @event,
        SemanticTypeSystem types)
    {
        var name = Identifiers.ToPascalCase(command.Name);
        var parameters = string.Join(", ", command.Properties.Select(_ => $"{types.Type(_.Type)} {Identifiers.ToPascalCase(_.Name)}"));
        var arguments = @event.Properties.Select(property =>
        {
            var mapping = produced.Mappings.Single(_ => _.TargetProperty == property.Id);
            var source = (SemanticResolvedExpression)mapping.Source;
            return Identifiers.ToPascalCase(command.Properties.Single(_ => _.Id == source.Target).Name);
        });

        builder.Attribute("Command")
            .Attribute("AllowAnonymous")
            .OpenBlock($"public record {name}({parameters})")
            .ExpressionMember($"public {Identifiers.ToPascalCase(@event.Name)} Handle()", $"new({string.Join(", ", arguments)})")
            .EndBlock()
            .BlankLine();
    }

    static void RenderEvent(CSharpCodeBuilder builder, SemanticEventContract @event, SemanticTypeSystem types)
    {
        var name = Identifiers.ToPascalCase(@event.Name);
        var parameters = string.Join(", ", @event.Properties.Select(_ => $"{types.Type(_.Type)} {Identifiers.ToPascalCase(_.Name)}"));
        builder.Summary($"The event that occurs when {Identifiers.ToWords(@event.Name)}.")
            .Attribute("EventType")
            .Line($"public record {name}({parameters});")
            .BlankLine();
    }

    static void RenderValidator(CSharpCodeBuilder builder, SemanticCommand command)
    {
        if (command.Validations.IsEmpty)
        {
            return;
        }

        var commandName = Identifiers.ToPascalCase(command.Name);
        builder.Summary($"Validates {Identifiers.ToWords(command.Name)}.")
            .OpenBlock($"public class {commandName}Validator : CommandValidator<{commandName}>")
            .OpenBlock($"public {commandName}Validator()");
        foreach (var rule in command.Validations)
        {
            var property = command.Properties.Single(_ => _.Id == rule.Property);
            var message = string.IsNullOrWhiteSpace(rule.Message)
                ? string.Empty
                : $".WithMessage({CSharpCodeBuilder.StringLiteral(rule.Message)})";
            builder.Line($"RuleFor(_ => _.{Identifiers.ToPascalCase(property.Name)}).NotEmpty(){message};");
        }

        builder.EndBlock().EndBlock();
    }
}
