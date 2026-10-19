// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;

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
        var builder = new CSharpCodeBuilder().Namespace(SliceNaming.Namespace(context.RootNamespace, located.Path));
        Configure(builder, located, context);
        RenderMembers(builder, located, context);
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), SliceNaming.FileName(located.Slice.Name)]);
        return new(path, builder.ToString())
        {
            Sources = [located.Slice.Id, .. located.Slice.Commands.Select(_ => _.Id), .. located.Slice.Events.Select(_ => _.Id)]
        };
    }

    internal static void Configure(CSharpCodeBuilder builder, LocatedSemanticSlice located, SemanticApplicationContext context)
    {
        var ownNamespace = SliceNaming.Namespace(context.RootNamespace, located.Path);
        builder.Using("Cratis.Arc.Authorization")
            .Using("Cratis.Arc.Commands.ModelBound")
            .Using("Cratis.Arc.Validation")
            .Using("Cratis.Chronicle.Events");
        if (located.Slice.Commands.Any(command => NeedsEventSequenceUsing(command, context)))
        {
            builder.Using("Cratis.Chronicle.EventSequences");
        }
        if (located.Slice.Commands.SelectMany(command => command.Properties).Concat(located.Slice.Events.SelectMany(_ => _.Properties))
            .Any(_ => SemanticTypeSystem.DeclarationNeedsCommon(_.Type)))
        {
            builder.Using(context.CommonNamespace);
        }

        foreach (var eventContract in located.Slice.Commands.SelectMany(command => command.Produces).Select(_ => context.Events[_.EventContract]))
        {
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(eventContract.Id).Path);
            if (!string.Equals(ownNamespace, eventNamespace, StringComparison.Ordinal))
            {
                builder.Using(eventNamespace);
            }
        }
    }

    internal static void RenderMembers(CSharpCodeBuilder builder, LocatedSemanticSlice located, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        foreach (var command in located.Slice.Commands)
        {
            RenderCommand(builder, command, context, types);
        }
        foreach (var declaredEvent in located.Slice.Events)
        {
            RenderEvent(builder, declaredEvent, types, context);
        }
        foreach (var command in located.Slice.Commands)
        {
            RenderValidator(builder, command, context);
        }
    }

    static bool NeedsEventSequenceUsing(SemanticCommand command, SemanticApplicationContext context) =>
        command.Produces.Length > 1 || command.Produces.Any(produced =>
            produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred }) ||
            !produced.Tags.IsEmpty || !context.Events[produced.EventContract].Tags.IsEmpty);

    static void RenderCommand(
        CSharpCodeBuilder builder,
        SemanticCommand command,
        SemanticApplicationContext context,
        SemanticTypeSystem types)
    {
        var locals = new SemanticHandleLocals(command);
        var results = new SemanticCommandResultRendering(command, context, locals);

        var severities = command.Validations.Select(rule => rule.Severity)
            .Concat(command.CodeValidations.Select(_ => SemanticValidationSeverity.Error))
            .Concat(command.Requirements.Select(requirement => requirement.Severity))
            .Concat(command.Properties.SelectMany(property => ReferencedValidations(property.Type, context, []).Select(rule => rule.Severity)))
            .ToArray();
        context.Docs(command.Id).Render(builder).Attribute("global::Cratis.Arc.Commands.ModelBound.CommandAttribute");
        if (results.Reads.HasReads)
        {
            builder.Attribute(SemanticDecisionReadRendering.ProtectionAttribute);
        }
        if (command.Route is { } route)
        {
            var source = context.Application.EventSources.Single(source => source.Id == route.Source);
            var stream = source.Streams.Single(stream => stream.Id == route.Stream);
            builder.Attribute($"global::Cratis.Arc.Chronicle.Commands.EventSourceAttribute<global::{SemanticEventSourceArtifactRenderer.Namespace(context)}.{SemanticEventSourceArtifactRenderer.ClassName(source)}>({SemanticEventSourceArtifactRenderer.StreamReference(source, stream, context)})");
        }
        if (severities.Length > 0)
        {
            // Screenplay rejects every validation failure; a caller must not loosen the modeled floor.
            // Keep this even for Error-only rules: Arc lets an unattributed caller allow errors, and
            // only attributed commands block Arc's Unknown severity (which Screenplay cannot model).
            var floor = severities.All(severity => severity == SemanticValidationSeverity.Error) ? "Error" : "Information";
            builder.Attribute($"global::Cratis.Arc.Commands.ModelBound.BlockOnValidationSeverityAttribute(global::Cratis.Arc.Validation.ValidationResultSeverity.{floor})");
        }

        results.RenderDeclaration(builder);
        var hasOccurrence = command.Produces.Any(produced => produced.Mappings.Any(mapping =>
            mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred }));
        var hasTags = command.Produces.Any(produced => !produced.Tags.IsEmpty || !context.Events[produced.EventContract].Tags.IsEmpty);
        var routing = new SemanticCommandRouteRendering(command, context, locals);
        var streamId = routing.StreamId();
        string EventValue(SemanticProducedEvent produced)
        {
            var @event = context.Events[produced.EventContract];
            var arguments = @event.Properties.Select(property =>
            {
                var mapping = produced.Mappings.Single(_ => _.TargetProperty == property.Id);
                if (mapping.Source is SemanticEventContextExpression) return locals.Name("occurred");
                if (mapping.Source is SemanticValueExpression value) return types.Value(value.Value, property.Type);
                if (mapping.Source is SemanticReadPathExpression readPath) return results.Reads.ReadPath(readPath);
                var commandProperty = command.Properties.Single(_ => _.Id == ((SemanticResolvedExpression)mapping.Source).Target);
                var expression = SemanticCommandResultRendering.PropertyValue(commandProperty, context);
                return ReducerCollection(@event, property, context) ? ImmutableSnapshot(expression, commandProperty.Type.IsOptional) : expression;
            });
            return $"new {types.EventType(@event)}({string.Join(", ", arguments)})";
        }

        string WrappedEvent(SemanticProducedEvent produced)
        {
            var target = (SemanticResolvedExpression)SemanticDestinations.Of(command, produced)!;
            var targetProperty = command.Properties.Single(_ => _.Id == target.Target);
            var @event = context.Events[produced.EventContract];
            var tags = @event.Tags.Concat(produced.Tags).ToArray();
            var metadata = new List<string>();
            if (streamId is not null)
            {
                metadata.Add($"EventStreamId = {streamId}");
            }
            if (hasOccurrence)
            {
                metadata.Add($"Occurred = {locals.Name("occurred")}");
            }

            if (tags.Length > 0)
            {
                metadata.Add($"Tags = [{string.Join(", ", tags.Select(tag => System.Text.Json.JsonSerializer.Serialize(tag)))}]");
            }

            var wrapper = $"new global::Cratis.Chronicle.EventSequences.EventForEventSourceId({types.EventSourceExpression(SemanticCommandResultRendering.PropertyValue(targetProperty, context), targetProperty.Type)}, {EventValue(produced)})";
            return metadata.Count == 0 ? wrapper : $"{wrapper} {{ {string.Join(", ", metadata)} }}";
        }

        results.RenderHandle(builder, WrappedEvent, () =>
        {
            if (routing.HasChecks)
            {
                var eventResult = command.Produces.Length == 1 ? "global::Cratis.Chronicle.EventSequences.EventForEventSourceId" : "global::System.Collections.Generic.IEnumerable<global::Cratis.Chronicle.EventSequences.EventForEventSourceId>";
                var result = $"global::OneOf.OneOf<{eventResult}, global::Cratis.Arc.Validation.ValidationResult>";
                builder.OpenBlock($"public {result} Handle({(hasOccurrence ? $"global::Cratis.Arc.IOperationContextAccessor {locals.Name("operation")}" : string.Empty)})");
                routing.RenderChecks(builder, result);
                if (hasOccurrence)
                {
                    builder.Line($"var {locals.Name("occurred")} = global::{context.RootNamespace}.GeneratedCommands.CommandReceiptTime.OccurredAtReceipt({locals.Name("operation")});");
                }
                var events = command.Produces.Length == 1 ? WrappedEvent(command.Produces[0]) : $"new global::Cratis.Chronicle.EventSequences.EventForEventSourceId[] {{ {string.Join(", ", command.Produces.Select(WrappedEvent))} }}";
                builder.Line($"return {result}.FromT0({events});").EndBlock();
            }
            else if (command.Produces.Length == 1 && !hasOccurrence && !hasTags && streamId is null)
            {
                var @event = context.Events[command.Produces[0].EventContract];
                var arguments = @event.Properties.Select(property =>
                {
                    var mapping = command.Produces[0].Mappings.Single(_ => _.TargetProperty == property.Id);
                    if (mapping.Source is SemanticValueExpression value) return types.Value(value.Value, property.Type);
                    var source = (SemanticResolvedExpression)mapping.Source;
                    var commandProperty = command.Properties.Single(_ => _.Id == source.Target);
                    var expression = SemanticCommandResultRendering.PropertyValue(commandProperty, context);
                    return ReducerCollection(@event, property, context) ? ImmutableSnapshot(expression, commandProperty.Type.IsOptional) : expression;
                });
                builder.ExpressionMember($"public {types.EventType(@event)} Handle()", $"new({string.Join(", ", arguments)})");
            }
            else if (!hasOccurrence)
            {
                var events = command.Produces.Select(WrappedEvent);
                if (command.Produces.Length == 1)
                {
                    builder.ExpressionMember("public global::Cratis.Chronicle.EventSequences.EventForEventSourceId Handle()", events.Single());
                }
                else
                {
                    builder.ExpressionMember("public global::System.Collections.Generic.IEnumerable<global::Cratis.Chronicle.EventSequences.EventForEventSourceId> Handle()", $"[{string.Join(", ", events)}]");
                }
            }
            else
            {
                var result = command.Produces.Length == 1 ? "global::Cratis.Chronicle.EventSequences.EventForEventSourceId" : "global::System.Collections.Generic.IEnumerable<global::Cratis.Chronicle.EventSequences.EventForEventSourceId>";

                // Chronicle's MongoDB event context retains UTC milliseconds. Normalize the payload
                // and append context together so a stored replay sees identical occurrence values.
                builder.OpenBlock($"public {result} Handle(global::Cratis.Arc.IOperationContextAccessor {locals.Name("operation")})")
                    .Line($"var {locals.Name("occurred")} = global::{context.RootNamespace}.GeneratedCommands.CommandReceiptTime.OccurredAtReceipt({locals.Name("operation")});")
                    .Line($"return {(command.Produces.Length == 1 ? WrappedEvent(command.Produces[0]) : $"[{string.Join(", ", command.Produces.Select(WrappedEvent))}]")};")
                    .EndBlock();
            }
        });

        builder.EndBlock().BlankLine();
        results.RenderResponseType(builder);
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

    static bool ReducerCollection(SemanticEventContract @event, SemanticProperty property, SemanticApplicationContext context) =>
        property.Type.IsCollection && context.Reducers.Any(reducer => reducer.Transitions.Any(transition => transition.EventContract == @event.Id));

    static string ImmutableSnapshot(string expression, bool optional)
    {
        var snapshot = $"global::System.Collections.Immutable.ImmutableArray.ToImmutableArray({expression})";
        return optional ? $"{expression} is null ? null : {snapshot}" : snapshot;
    }

    static void RenderEvent(CSharpCodeBuilder builder, SemanticEventContract @event, SemanticTypeSystem types, SemanticApplicationContext context)
    {
        var name = Identifiers.ToPascalCase(@event.Name);
        var parameters = string.Join(", ", @event.Properties.Select(property =>
            $"{types.Type(property.Type, ReducerCollection(@event, property, context))} {Identifiers.ToPascalCase(property.Name)}"));
        context.Docs(@event.Id).Render(builder, $"The event that occurs when {Identifiers.ToWords(@event.Name)}.")
            .Attribute(@event.StoredName is { } stored
                ? $"global::Cratis.Chronicle.Events.EventTypeAttribute({CSharpCodeBuilder.StringLiteral(stored)})"
                : "global::Cratis.Chronicle.Events.EventTypeAttribute")
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
            .Select(mapping => mapping.Source).OfType<SemanticResolvedExpression>().Select(source => source.Target).Distinct()
            .Where(id => !command.Properties.Any(property => property.Id == id && property.IsGenerated) &&
                !command.Validations.Any(rule => rule.Property == id && rule.Kind == SemanticValidationRuleKind.NotEmpty))
            .ToArray();

        // A protected command evaluates its requirements inside Handle, with the decision reads, not in the parameterless validator.
        var requirements = command.Reads.IsDefaultOrEmpty ? command.Requirements : [];
        if (command.Validations.IsEmpty && command.CodeValidations.IsEmpty && requirements.IsEmpty && constrained.Length == 0)
        {
            return;
        }

        var commandName = Identifiers.ToPascalCase(command.Name);
        builder.Summary($"Validates {Identifiers.ToWords(command.Name)}.")
            .OpenBlock($"public class {commandName}Validator : global::Cratis.Arc.Commands.CommandValidator<{commandName}>")
            .OpenBlock($"public {commandName}Validator()");
        var raw = SemanticDateComparisonAdmission.Raw(context.Application, command);
        foreach (var rule in command.Validations)
        {
            var property = command.Properties.Single(_ => _.Id == rule.Property);
            var primitive = SemanticValidationRendering.UnderlyingPrimitive(property.Type, context);
            var wrapped = property.Type.Kind == SemanticTypeReferenceKind.Concept && !raw.Contains(property.Id);
            if (SemanticValidationContextRuntime.IsBody(rule))
            {
                SemanticValidationContextRuntime.RenderRule(builder, context, rule, command.Id, property);
            }
            else
            {
                SemanticValidationRendering.Render(builder, rule, property.Name, primitive, property.Type.IsCollection, false, wrapped, property.Type.IsOptional, context.RootNamespace, CommandConceptRendering(rule, property, context), generatedMember: context.MemberNames.Property(property));
            }
        }

        foreach (var validation in command.CodeValidations)
        {
            SemanticValidationContextRuntime.RenderIterator(builder, context, validation.RequirementId, command.Id);
        }

        foreach (var id in constrained)
        {
            var property = command.Properties.Single(candidate => candidate.Id == id);
            builder.Line($"RuleFor(_ => _.{context.MemberNames.Property(property)}).NotNull().WithMessage(\"A constrained value is required.\");");
        }

        foreach (var requirement in requirements)
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

    static SemanticValidationRendering.ConceptRendering CommandConceptRendering(SemanticValidationRule rule, SemanticProperty property, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var primitive = SemanticValidationRendering.UnderlyingPrimitive(property.Type, context);
        var optionalType = property.Type is { Kind: SemanticTypeReferenceKind.Concept, IsOptional: true, IsCollection: false } &&
            primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean or SemanticPrimitiveType.Date
                ? types.Type(property.Type)
                : null;
        var enumOperand = property.Type.Kind == SemanticTypeReferenceKind.Concept && !context.Concepts[property.Type.Target].Values.IsEmpty
            ? types.Value(rule.Operand!, property.Type)
            : null;
        return new(optionalType, enumOperand);
    }
}
