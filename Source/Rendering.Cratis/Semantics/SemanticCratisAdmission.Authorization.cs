// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static bool ValidateCommandAuthorization(SemanticCommand command, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (command.Authorization is not null)
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Command '{command.Name}' is authorized by a policy, which the Cratis ESM planner does not render yet. Rendering it would let every caller execute it.", command.Id));
            return false;
        }

        return true;
    }

    static bool ValidateQueryAuthorization(SemanticKeyedQuery query, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (query.Authorization is not null)
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Query '{query.Name}' is authorized by a policy, which the Cratis ESM planner does not render yet. The rendered query allows anonymous callers.", query.Id));
            return false;
        }

        return true;
    }
}
