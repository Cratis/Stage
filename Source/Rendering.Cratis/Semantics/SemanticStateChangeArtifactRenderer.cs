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
        if (command.Produces.Length > 1 || command.Produces.Any(produced =>
            produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred }) ||
            !produced.Tags.IsEmpty || !context.Events[produced.EventContract].Tags.IsEmpty))
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
        return new(path, builder.ToString())
        {
            Sources = [located.Slice.Id, .. located.Slice.Commands.Select(_ => _.Id), .. located.Slice.Events.Select(_ => _.Id)]
        };
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

        var severities = command.Validations.Select(rule => rule.Severity)
            .Concat(command.Requirements.Select(requirement => requirement.Severity))
            .Concat(command.Properties.SelectMany(property => ReferencedValidations(property.Type, context, []).Select(rule => rule.Severity)))
            .ToArray();
        builder.Attribute("Command");
        if (severities.Length > 0)
        {
            // Screenplay rejects every validation failure; a caller must not loosen the modeled floor.
            var floor = severities.All(severity => severity == SemanticValidationSeverity.Error) ? "Error" : "Information";
            builder.Attribute($"BlockOnValidationSeverity(ValidationResultSeverity.{floor})");
        }

        builder.Attribute(SemanticAuthorizationAttributes.For(command))
            .OpenBlock($"public record {name}({parameters}) : ICanProvideEventSourceId")
            .Line("/// <inheritdoc/>")
            .ExpressionMember("public EventSourceId GetEventSourceId()", destinationExpression)
            .BlankLine();
        var hasOccurrence = command.Produces.Any(produced => produced.Mappings.Any(mapping =>
            mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred }));
        var hasTags = command.Produces.Any(produced => !produced.Tags.IsEmpty || !context.Events[produced.EventContract].Tags.IsEmpty);
        string EventValue(SemanticProducedEvent produced)
        {
            var @event = context.Events[produced.EventContract];
            var arguments = @event.Properties.Select(property =>
            {
                var mapping = produced.Mappings.Single(_ => _.TargetProperty == property.Id);
                return mapping.Source is SemanticEventContextExpression ? "occurred" :
                    Identifiers.ToPascalCase(command.Properties.Single(_ => _.Id == ((SemanticResolvedExpression)mapping.Source).Target).Name);
            });
            return $"new {Identifiers.ToPascalCase(@event.Name)}({string.Join(", ", arguments)})";
        }

        string WrappedEvent(SemanticProducedEvent produced)
        {
            var target = (SemanticResolvedExpression)SemanticDestinations.Of(command, produced)!;
            var targetProperty = command.Properties.Single(_ => _.Id == target.Target);
            var @event = context.Events[produced.EventContract];
            var tags = @event.Tags.Concat(produced.Tags).ToArray();
            var metadata = new List<string>();
            if (hasOccurrence)
            {
                metadata.Add("Occurred = occurred");
            }

            if (tags.Length > 0)
            {
                metadata.Add($"Tags = [{string.Join(", ", tags.Select(tag => System.Text.Json.JsonSerializer.Serialize(tag)))}]");
            }

            var wrapper = $"new EventForEventSourceId({types.EventSourceExpression(Identifiers.ToPascalCase(targetProperty.Name), targetProperty.Type)}, {EventValue(produced)})";
            return metadata.Count == 0 ? wrapper : $"{wrapper} {{ {string.Join(", ", metadata)} }}";
        }

        if (command.Produces.Length == 1 && !hasOccurrence && !hasTags)
        {
            var @event = context.Events[command.Produces[0].EventContract];
            builder.ExpressionMember($"public {Identifiers.ToPascalCase(@event.Name)} Handle()", EventValue(command.Produces[0]));
        }
        else if (!hasOccurrence)
        {
            var events = command.Produces.Select(WrappedEvent);
            if (command.Produces.Length == 1)
            {
                builder.ExpressionMember("public EventForEventSourceId Handle()", events.Single());
            }
            else
            {
                builder.ExpressionMember("public IEnumerable<EventForEventSourceId> Handle()", $"[{string.Join(", ", events)}]");
            }
        }
        else
        {
            var result = command.Produces.Length == 1 ? "EventForEventSourceId" : "IEnumerable<EventForEventSourceId>";
            builder.OpenBlock($"public {result} Handle()")
                .Line("var occurred = DateTimeOffset.UtcNow;")
                .Line($"return {(command.Produces.Length == 1 ? WrappedEvent(command.Produces[0]) : $"[{string.Join(", ", command.Produces.Select(WrappedEvent))}]")};")
                .EndBlock();
        }

        builder.EndBlock().BlankLine();
    }

    static IEnumerable<SemanticValidationRule> ReferencedValidations(
        SemanticTypeReference type,
        SemanticApplicationContext context,
        HashSet<SemanticId> visited)
    {
        if (type.Kind == SemanticTypeReferenceKind.Concept && context.Concepts.TryGetValue(type.Target, out var concept))
        {
            return concept.Validations;
        }

        if (type.Kind == SemanticTypeReferenceKind.CompositeType && visited.Add(type.Target) &&
            context.Types.TryGetValue(type.Target, out var composite))
        {
            return composite.Properties.SelectMany(property => ReferencedValidations(property.Type, context, visited));
        }

        return [];
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
        var constrained = context.Constraints.Select(_ => _.Constraint)
            .Where(constraint => constraint.Kind == SemanticConstraintKind.UniquePropertyValue)
            .SelectMany(constraint => constraint.Targets)
            .SelectMany(target => command.Produces.Where(produced => produced.EventContract == target.EventContract)
                .SelectMany(produced => produced.Mappings.Where(mapping => target.Properties.Contains(mapping.TargetProperty))))
            .Select(mapping => ((SemanticResolvedExpression)mapping.Source).Target).Distinct()
            .Where(id => !command.Validations.Any(rule => rule.Property == id && rule.Kind == SemanticValidationRuleKind.NotEmpty))
            .ToArray();
        if (command.Validations.IsEmpty && command.Requirements.IsEmpty && constrained.Length == 0)
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
            SemanticValidationRendering.Render(builder, rule, property.Name, primitive, property.Type.IsCollection, false, property.Type.Kind == SemanticTypeReferenceKind.Concept, property.Type.IsOptional, context.RootNamespace);
        }

        foreach (var id in constrained)
        {
            var property = command.Properties.Single(candidate => candidate.Id == id);
            builder.Line($"RuleFor(_ => _.{Identifiers.ToPascalCase(property.Name)}).NotNull().WithMessage(\"A constrained value is required.\");");
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
