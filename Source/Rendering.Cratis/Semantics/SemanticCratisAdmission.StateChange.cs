// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

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
        if (slice.Commands.Length != 1)
        {
            diagnostics.Add(Error("STAGE-ESM-004", $"State-change slice '{slice.Name}' must contain exactly one command.", slice.Id));
            return;
        }

        var command = slice.Commands[0];
        if (!ValidateCommandAuthorization(context, command, diagnostics))
        {
            return;
        }

        if (!command.CodeValidations.IsEmpty || command.Validations.Any(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation))
        {
            var ids = command.CodeValidations.Select(validation => validation.RequirementId)
                .Concat(command.Validations.Where(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation)
                    .Select(rule => rule.RequirementId));
            diagnostics.Add(Error("STAGE-ESM-005", $"Command '{command.Name}' contains validation implementation bodies ({string.Join(", ", ids)}): Arc does not supply RuleContext.Occurred (received-at) to a generated validator, and Stage does not yet enforce the pure capability.", command.Id));
            return;
        }

        if (slice.Events.Any(@event => @event.Revision != EventContractRevision.Initial ||
                @event.Properties.Any(property => !TypeExists(context, property.Type) || property.Type.IsOptional)) ||
            command.Properties.Any(_ => !TypeExists(context, _.Type)) ||
            !command.Validations.All(rule => SemanticValidationRendering.CanRender(rule, context)) ||
            command.Validations.Any(rule => command.Properties.Single(property => property.Id == rule.Property).Type is
                { Kind: SemanticTypeReferenceKind.Concept, IsOptional: true } type &&
                context.Concepts[type.Target].Primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean) ||
            !command.Requirements.All(_ => SemanticRequirementRendering.CanRender(_, command, context)) ||
            (!command.Requirements.IsEmpty && command.Properties.Any(_ => HasValidatedConcept(context, _.Type, []))) ||
            command.Validations.Any(_ =>
            {
                var property = command.Properties.Single(p => p.Id == _.Property);
                return property.Type.Kind == SemanticTypeReferenceKind.Concept && !context.Concepts[property.Type.Target].Values.IsEmpty;
            }) ||
            command.Properties.Any(_ =>
                (_.Type.Kind == SemanticTypeReferenceKind.Concept && _.Type.IsOptional &&
                 !context.Concepts[_.Type.Target].Validations.IsEmpty) ||
                (_.Type.Kind == SemanticTypeReferenceKind.CompositeType && HasOptionalRequiredConcept(context, _.Type, []))) ||
            command.Produces.IsEmpty)
        {
            diagnostics.Add(Error("STAGE-ESM-005", $"Command '{command.Name}' exceeds the first Cratis command capability.", command.Id));
            return;
        }

        if (command.Produces.Any(produced => produced.Mappings.Any(_ => _.Source is SemanticEventContextExpression)))
        {
            diagnostics.Add(Error("STAGE-ESM-013", $"Produced event of command '{command.Name}' maps a command occurrence value ($context). Chronicle assigns the occurrence when it appends, so a Cratis command cannot put the same value in the event payload.", command.Id));
            return;
        }

        if (command.Produces.Any(produced =>
            !context.Events.TryGetValue(produced.EventContract, out var @event) || produced.Condition is not null ||
            produced.When is not null || !produced.Tags.IsEmpty || !@event.Tags.IsEmpty ||
            !IsProperty(SemanticDestinations.Of(command, produced), SemanticExpressionRootKind.Command, command.Properties.Where(_ => _.IsIdentifier).Select(_ => _.Id)) ||
            @event.Revision != EventContractRevision.Initial || @event.Properties.Any(_ => !TypeExists(context, _.Type) || _.Type.IsOptional) ||
            !MappingsMatch(produced.Mappings, @event.Properties, command.Properties, SemanticExpressionRootKind.Command)))
        {
            diagnostics.Add(Error("STAGE-ESM-006", $"Produced event of command '{command.Name}' cannot be rendered without changing its destination or mappings.", command.Id));
        }
    }

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
            (property.Type.Kind == SemanticTypeReferenceKind.Concept && property.Type.IsOptional &&
             !context.Concepts[property.Type.Target].Validations.IsEmpty) ||
            HasOptionalRequiredConcept(context, property.Type, visited));
    }
}
