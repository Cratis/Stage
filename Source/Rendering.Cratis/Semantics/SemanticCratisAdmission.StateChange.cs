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
        if (!ValidateCommandAuthorization(command, diagnostics))
        {
            return;
        }

        if (slice.Events.Any(@event => @event.Revision != EventContractRevision.Initial ||
                @event.Properties.Any(property => !TypeExists(context, property.Type) || property.Type.IsOptional)) ||
            command.Properties.Any(_ => !TypeExists(context, _.Type)) ||
            !command.Validations.All(IsRenderableValidation) ||
            !command.Requirements.IsEmpty ||
            command.Produces.Length != 1)
        {
            diagnostics.Add(Error("STAGE-ESM-005", $"Command '{command.Name}' exceeds the first Cratis command capability.", command.Id));
            return;
        }

        var produced = command.Produces[0];
        if (produced.Mappings.Any(_ => _.Source is SemanticEventContextExpression))
        {
            diagnostics.Add(Error("STAGE-ESM-013", $"Produced event of command '{command.Name}' maps a command occurrence value ($context). Chronicle assigns the occurrence when it appends, so a Cratis command cannot put the same value in the event payload.", command.Id));
            return;
        }

        if (!context.Events.TryGetValue(produced.EventContract, out var @event) || produced.Condition is not null ||
            produced.When is not null || !produced.Tags.IsEmpty || !@event.Tags.IsEmpty ||
            !IsProperty(SemanticDestinations.Of(command, produced), SemanticExpressionRootKind.Command, command.Properties.Where(_ => _.IsIdentifier).Select(_ => _.Id)) ||
            @event.Revision != EventContractRevision.Initial || @event.Properties.Any(_ => !TypeExists(context, _.Type) || _.Type.IsOptional) ||
            !MappingsMatch(produced.Mappings, @event.Properties, command.Properties, SemanticExpressionRootKind.Command))
        {
            diagnostics.Add(Error("STAGE-ESM-006", $"Produced event of command '{command.Name}' cannot be rendered without changing its destination or mappings.", command.Id));
        }
    }
}
