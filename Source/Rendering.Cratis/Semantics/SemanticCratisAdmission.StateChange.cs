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
            diagnostics.Add(Error("STAGE-ESM-012", $"Command '{command.Name}' has property names that collide in generated C#.", command.Id));
            return;
        }

        if (!ValidateCommandAuthorization(context, command, diagnostics))
        {
            return;
        }

        if (!ValidateCommandCapabilities(context, slice, command, diagnostics))
        {
            return;
        }

        if (CommandResultError(context, slice, command) is { } resultError)
        {
            diagnostics.Add(resultError);
            return;
        }

        if (command.Produces.Any(produced =>
            !context.Events.TryGetValue(produced.EventContract, out var @event) || produced.Condition is not null ||
            !HasRenderableProductionCondition(produced, command, context) ||
            @event.Tags.Concat(produced.Tags).Distinct(StringComparer.Ordinal).Count() != @event.Tags.Length + produced.Tags.Length ||
            !IsProperty(SemanticDestinations.Of(command, produced), SemanticExpressionRootKind.Command, command.Properties.Where(_ => _.IsIdentifier).Select(_ => _.Id)) ||
            @event.Properties.Any(_ => !TypeExists(context, _.Type) || _.Type.IsOptional) ||
            produced.Mappings.Length != @event.Properties.Length ||
            !@event.Properties.All(property => produced.Mappings.Any(mapping => mapping.TargetProperty == property.Id &&
                (IsProperty(mapping.Source, SemanticExpressionRootKind.Command, command.Properties.Select(_ => _.Id)) ||
                 (mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } occurrence && occurrence.Type == property.Type) ||
                 mapping.Source is SemanticValueExpression)))))
        {
            diagnostics.Add(Error("STAGE-ESM-006", $"Produced event of command '{command.Name}' cannot be rendered without changing its destination or mappings.", command.Id));
        }
    }

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
