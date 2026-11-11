// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static bool TryRefuseDeniedOperation(SemanticApplicationContext context, SemanticSpecification specification, ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        if (context.DeniedOperations.Count == 0) return false;

        foreach (var command in InvokedCommands(context, specification).Where(context.DeniedOperations.ContainsKey).Distinct())
        {
            diagnostics.Add(new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, $"Specification '{specification.Name}' cannot render: command '{context.Commands[command].Name}' denies every invocation (STAGE-DEGRADE-002).", specification.Id));
            return true;
        }
        return false;
    }

    static IEnumerable<SemanticId> InvokedCommands(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is { } when) return [when.Command];
        if (specification.WhenAppended is null && specification.WhenRedelivered is null) return [];

        var active = ReactionCandidates(context, specification)
            .Where(candidate => specification.ThenNoEvents || specification.WhenRedelivered is not null || candidate.Fires).ToArray();
        return active.Length == 1 ? active[0].Trigger.Invokes.Select(invocation => invocation.Command) : [];
    }
}
