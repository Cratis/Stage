// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static void ValidateReactionIdentity(SemanticApplicationContext context, SemanticReaction reaction, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (reaction.RunsAs is null)
        {
            ReportInferredReactionIdentity(context, reaction, diagnostics);
            return;
        }

        foreach (var command in reaction.Triggers.SelectMany(trigger => trigger.Invokes)
            .Select(invocation => context.Commands[invocation.Command]).DistinctBy(command => command.Id)
            .Where(command => command.Authorization is not null))
        {
            var authorization = command.Authorization!;
            if (Claims(authorization, context.Application.Policies).Any())
            {
                diagnostics.Add(Error("STAGE-ESM-034", $"Reaction '{reaction.Name}' runs as system and invokes '{command.Name}', whose authorization reads claims; Screenplay treats system claims as unknown while Arc's system principal carries [System] subject claims (Screenplay 0043).", reaction.Id));
            }
            else if (OpaquePolicies(authorization, context.Application.Policies).Any())
            {
                diagnostics.Add(Error("STAGE-ESM-034", $"Reaction '{reaction.Name}' runs as system and invokes '{command.Name}', whose opaque authorization may read claims; use a role-only or authenticated-only gate to preserve Screenplay 0043.", reaction.Id));
            }
        }
    }

    static void ReportInferredReactionIdentity(SemanticApplicationContext context, SemanticReaction reaction, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (!SemanticReactionIdentity.InvokesUnauthorizedCommand(context, reaction)) return;
        diagnostics.Add(new(
            "STAGE-ESM-055",
            ArtifactRenderDiagnosticSeverity.Information,
            $"Reaction '{reaction.Name}' has no runs as and invokes a command with no authorize clause, which renders [Authorize]; " +
            "Stage renders the reactor with [ExecuteCommandsAsSystem] (the system principal, no roles) so the command is still accepted.",
            reaction.Id));
    }
}
