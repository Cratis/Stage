// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static void ValidateRefusalValues(SemanticApplicationContext context, SemanticReaction reaction, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var invocation in reaction.Triggers.SelectMany(trigger => trigger.Invokes))
        {
            var members = invocation.OnRefused.SelectMany(branch => branch.Produces).SelectMany(produced => produced.Mappings)
                .Select(mapping => mapping.Source).OfType<SemanticRefusalExpression>().Select(refusal => refusal.Member).ToHashSet(StringComparer.Ordinal);
            var command = context.Commands[invocation.Command];
            var constraints = context.Constraints.Select(selected => selected.Constraint)
                .Where(constraint => constraint.Targets.Any(target => command.Produces.Any(produced => produced.EventContract == target.EventContract))).ToArray();
            if (members.Contains("constraint") && constraints.Length > 1)
            {
                diagnostics.Add(Error("STAGE-ESM-039", $"Reaction '{reaction.Name}' maps $refusal.constraint from '{command.Name}', which can violate multiple constraints; Arc 22.88.0 exposes the first returned violation, not Screenplay's candidate/sorted-name order.", reaction.Id));
            }
            if (!members.Contains("message")) continue;

            var messages = command.Validations.Select(rule => rule.Message).Concat(command.Requirements.Select(requirement => requirement.Message))
                .Concat(command.Properties.SelectMany(property => RefusalConceptMessages(context, property.Type, [])))
                .Concat(constraints.Select(constraint => constraint.Message));
            if (messages.Any(message => message?.StartsWith("$strings.", StringComparison.Ordinal) == true))
            {
                diagnostics.Add(Error("STAGE-ESM-039", $"Reaction '{reaction.Name}' maps $refusal.message from '{command.Name}', which can reject with a localized $strings message; Arc 22.88.0 exposes resolved prose while Screenplay preserves the unresolved key.", reaction.Id));
            }
        }
    }

    static IEnumerable<string?> RefusalConceptMessages(SemanticApplicationContext context, SemanticTypeReference type, HashSet<SemanticId> visited)
    {
        if (type.Kind == SemanticTypeReferenceKind.Concept)
        {
            return context.Concepts[type.Target].Validations.Select(rule => rule.Message);
        }
        if (type.Kind == SemanticTypeReferenceKind.CompositeType && visited.Add(type.Target))
        {
            return context.Types[type.Target].Properties.SelectMany(property => RefusalConceptMessages(context, property.Type, visited));
        }

        return [];
    }
}
