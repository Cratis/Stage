// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits the supported state-change shape.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    static void ValidateStateChange(
        SemanticApplicationContext context,
        SemanticSlice slice,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (slice.Commands.IsEmpty)
        {
            diagnostics.Add(Error("STAGE-ESM-004", $"State-change slice '{slice.Name}' must contain at least one command.", slice.Id));
            return;
        }

        foreach (var command in slice.Commands)
        {
            ValidateCommand(context, slice, command, diagnostics);
        }
    }

    static void ValidateCommand(
        SemanticApplicationContext context,
        SemanticSlice slice,
        SemanticCommand command,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (!GeneratedPascalCase.CommandMembersAreUnique(command.Name, command.Properties.Select(property => property.Name)))
        {
            var duplicates = command.Properties.GroupBy(property => Identifiers.ToPascalCase(property.Name), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1).SelectMany(group => group.Select(property => property.Name)).ToArray();
            var declaration = duplicates.Length == 0
                ? $"type name '{command.Name}'"
                : $"properties [{string.Join(", ", duplicates.Select(name => $"'{command.Name}.{name}'"))}]";
            var collision = duplicates.Length == 0 ? "collides with a generated member" : "collide with each other";
            diagnostics.Add(Error("STAGE-ESM-012", $"Command '{command.Name}' cannot render in generated C#: {declaration} {collision}.", command.Id));
            return;
        }

        foreach (var property in command.Properties.Where(property => !property.IsGenerated && context.MemberNames.Property(property) != Identifiers.ToPascalCase(property.Name)))
        {
            diagnostics.Add(new(
                "STAGE-ESM-050",
                ArtifactRenderDiagnosticSeverity.Information,
                $"Command '{command.Name}' property '{property.Name}' is emitted as '{context.MemberNames.Property(property)}' (including its wire and proxy field).",
                command.Id));
        }

        if (command.Response is SemanticRecordCommandResponse && context.ResponseType(command) != $"{Identifiers.ToPascalCase(command.Name)}Response")
        {
            diagnostics.Add(new(
                "STAGE-ESM-050",
                ArtifactRenderDiagnosticSeverity.Information,
                $"Command '{command.Name}' response record is emitted as '{context.ResponseType(command)}' (including its proxy type); its fields keep their names.",
                command.Id));
        }

        if (!ValidateCommandAuthorization(context, command, diagnostics))
        {
            return;
        }

        if (!ValidateCommandCapabilities(context, slice, command, diagnostics))
        {
            return;
        }

        SemanticCommandBodyAdmission.Verify(command, context, diagnostics);

        if (CommandResultError(command) is { } resultError)
        {
            diagnostics.Add(resultError);
            return;
        }

        if (command.Produces.Any(produced =>
            !context.Events.TryGetValue(produced.EventContract, out var @event) || produced.Condition is not null ||
            !HasRenderableProductionCondition(produced, command, context) ||
            @event.Tags.Concat(produced.Tags).Distinct(StringComparer.Ordinal).Count() != @event.Tags.Length + produced.Tags.Length ||
            !IsProperty(SemanticDestinations.Of(command, produced), SemanticExpressionRootKind.Command, EventSourceProperties(command)) ||
            @event.Properties.Any(_ => !TypeExists(context, _.Type, @event.Id) || _.Type.IsOptional) ||
            InvalidPayloadMappings(command, produced, @event)))
        {
            diagnostics.Add(Error("STAGE-ESM-006", $"Produced event of command '{command.Name}' cannot be rendered without changing its destination or mappings.", command.Id));
        }
    }

    // ESM v30 lets a production address any required, non-collection scalar command property as its event source, not only the identifier.
    static IEnumerable<SemanticId> EventSourceProperties(SemanticCommand command) =>
        command.Properties.Where(property => property.IsIdentifier ||
            (!property.Type.IsOptional && !property.Type.IsCollection && property.Type.Kind is SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept))
            .Select(property => property.Id);

    static bool HasRenderableProductionCondition(SemanticProducedEvent produced, SemanticCommand command, SemanticApplicationContext context) =>
        produced.When is null || SemanticConditionRendering.CanRender(produced.When, command.Properties, context, numericOrderingOnly: true, dates: true);

    static bool HasValidatedConcept(SemanticApplicationContext context, SemanticTypeReference type, HashSet<SemanticId> visited) =>
        type.Kind switch
        {
            SemanticTypeReferenceKind.Concept => !context.Concepts[type.Target].Validations.IsEmpty,
            SemanticTypeReferenceKind.CompositeType when visited.Add(type.Target) =>
                context.Types[type.Target].Properties.Any(_ => HasValidatedConcept(context, _.Type, visited)),
            _ => false
        };

    static bool HasOptionalRequiredConcept(SemanticApplicationContext context, SemanticTypeReference type, HashSet<SemanticId> visited)
    {
        if (type.Kind != SemanticTypeReferenceKind.CompositeType || !visited.Add(type.Target))
        {
            return false;
        }

        return context.Types[type.Target].Properties.Any(property =>
            HasUnsupportedOptionalConcept(context, property.Type) ||
            HasOptionalRequiredConcept(context, property.Type, visited));
    }
}
