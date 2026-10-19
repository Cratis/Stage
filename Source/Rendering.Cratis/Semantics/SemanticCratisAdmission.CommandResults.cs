// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static ArtifactRenderDiagnostic? CommandResultError(SemanticCommand command)
    {
        if (command.Produces.Any(produced => produced.Mappings.Any(mapping =>
            mapping.Source is SemanticEventContextExpression { Value: not SemanticEventContextValueKind.Occurred })))
        {
            return Error("STAGE-ESM-013", $"Produced event of command '{command.Name}' maps an unsupported command occurrence value ($context); only $context.occurred can be captured at the command boundary.", command.Id);
        }

        return null;
    }

    // A command with protected reads may only decide: Arc still validates the reads it declared, and nothing is committed.
    static bool HasCommandOutcome(SemanticCommand command) => !command.Produces.IsEmpty || command.Response is not null || !command.Reads.IsDefaultOrEmpty;
}
