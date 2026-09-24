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
        var ownNamespace = SliceNaming.Namespace(context.RootNamespace, located.Path);
        var builder = new CSharpCodeBuilder()
            .Namespace(ownNamespace)
            .Using("Cratis.Arc.Authorization")
            .Using("Cratis.Arc.Commands.ModelBound")
            .Using("Cratis.Arc.Validation")
            .Using("Cratis.Chronicle.Events");
        if (command.Produces.Length > 1)
        {
            builder.Using("Cratis.Chronicle.EventSequences");
        }
        if (command.Properties.Concat(located.Slice.Events.SelectMany(_ => _.Properties))
            .Any(_ => SemanticTypeSystem.DeclarationNeedsCommon(_.Type)))
        {
            builder.Using($"{context.RootNamespace}.Common");
        }

        foreach (var eventContract in command.Produces.Select(_ => context.Events[_.EventContract]))
        {
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(eventContract.Id).Path);
            if (!string.Equals(ownNamespace, eventNamespace, StringComparison.Ordinal))
            {
                builder.Using(eventNamespace);
            }
        }

        RenderCommand(builder, command, context, types);
        foreach (var declaredEvent in located.Slice.Events)
        {
            RenderEvent(builder, declaredEvent, types);
        }

        RenderValidator(builder, command, context);
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), SliceNaming.FileName(located.Slice.Name)]);
        return new(path, builder.ToString());
    }

    static void RenderCommand(
        CSharpCodeBuilder builder,
        SemanticCommand command,
        SemanticApplicationContext context,
        SemanticTypeSystem types)
    {
        var name = Identifiers.ToPascalCase(command.Name);
        var parameters = string.Join(", ", command.Properties.Select(_ => $"{types.Type(_.Type)} {Identifiers.ToPascalCase(_.Name)}"));
        var destination = (SemanticResolvedExpression)SemanticDestinations.Of(command, command.Produces[0])!;
        var destinationProperty = command.Properties.Single(_ => _.Id == destination.Target);
        var destinationExpression = types.EventSourceExpression(Identifiers.ToPascalCase(destinationProperty.Name), destinationProperty.Type);

        builder.Attribute("Command")
            .Attribute(SemanticAuthorizationAttributes.For(command))
            .OpenBlock($"public record {name}({parameters}) : ICanProvideEventSourceId")
            .Line("/// <inheritdoc/>")
            .ExpressionMember("public EventSourceId GetEventSourceId()", destinationExpression)
            .BlankLine();
        if (command.Produces.Length == 1)
        {
            var @event = context.Events[command.Produces[0].EventContract];
            var arguments = @event.Properties.Select(property =>
            {
                var mapping = command.Produces[0].Mappings.Single(_ => _.TargetProperty == property.Id);
                var source = (SemanticResolvedExpression)mapping.Source;
                return Identifiers.ToPascalCase(command.Properties.Single(_ => _.Id == source.Target).Name);
            });
            builder.ExpressionMember($"public {Identifiers.ToPascalCase(@event.Name)} Handle()", $"new({string.Join(", ", arguments)})");
        }
        else
        {
            var events = command.Produces.Select(produced =>
            {
                var @event = context.Events[produced.EventContract];
                var arguments = @event.Properties.Select(property =>
                {
                    var mapping = produced.Mappings.Single(_ => _.TargetProperty == property.Id);
                    var source = (SemanticResolvedExpression)mapping.Source;
                    return Identifiers.ToPascalCase(command.Properties.Single(_ => _.Id == source.Target).Name);
                });
                var value = $"new {Identifiers.ToPascalCase(@event.Name)}({string.Join(", ", arguments)})";
                var target = (SemanticResolvedExpression)SemanticDestinations.Of(command, produced)!;
                var targetProperty = command.Properties.Single(_ => _.Id == target.Target);
                return $"new EventForEventSourceId({types.EventSourceExpression(Identifiers.ToPascalCase(targetProperty.Name), targetProperty.Type)}, {value})";
            });
            builder.ExpressionMember("public IEnumerable<EventForEventSourceId> Handle()", $"[{string.Join(", ", events)}]");
        }

        builder.EndBlock().BlankLine();
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

    static void RenderValidator(CSharpCodeBuilder builder, SemanticCommand command, SemanticApplicationContext context)
    {
        if (command.Validations.IsEmpty && command.Requirements.IsEmpty)
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
            var primitive = SemanticValidationRendering.UnderlyingPrimitive(property.Type, context);
            SemanticValidationRendering.Render(builder, rule, property.Name, primitive, property.Type.IsCollection, false, property.Type.Kind == SemanticTypeReferenceKind.Concept, property.Type.IsOptional);
        }

        foreach (var requirement in command.Requirements)
        {
            SemanticRequirementRendering.Render(builder, requirement, command, context);
        }

        builder.EndBlock();
        if (command.Validations.Any(_ => _.Kind == SemanticValidationRuleKind.Matches))
        {
            SemanticValidationRendering.RenderMatchHelper(builder);
        }

        builder.EndBlock();
    }
}
