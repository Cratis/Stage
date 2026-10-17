// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits the command Date and DateTime comparisons (ESM v26) the Cratis target renders exactly.
/// </summary>
/// <remarks>
/// A compared DateTime command property carries Arc's explicit-offset marker so input without an offset is refused after
/// authorization, as the reference evaluator refuses it. The marker applies to DateTime and DateTimeOffset properties only,
/// so a compared property typed through a concept binds as the concept's DateTimeOffset and converts to the concept
/// implicitly where an event or rule uses it. That is exact only for a concept without compliance.
/// Pending Arc ask: an explicit-offset marker for concept-wrapped dates would remove the raw binding workaround.
/// Arc names the failing member in camel case, so a property whose declared name is not camel case is refused.
/// </remarks>
internal static class SemanticDateComparisonAdmission
{
    internal static IEnumerable<ArtifactRenderDiagnostic> Verify(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices) =>
        slices.SelectMany(located => located.Slice.Commands).SelectMany(command => Verify(context, command));

    /// <summary>
    /// Finds the compared DateTime command properties, which carry Arc's explicit-offset marker.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <param name="command">The command.</param>
    /// <returns>The marked property identities.</returns>
    internal static IReadOnlySet<SemanticId> Marked(SemanticApplication application, SemanticCommand command) =>
        SemanticDateComparisons.Participants(application, command).Where(participant => participant.Value == SemanticPrimitiveType.DateTime)
            .Select(participant => participant.Key).ToHashSet();

    /// <summary>
    /// Finds the marked properties typed through a concept, which bind as the concept's DateTimeOffset.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <param name="command">The command.</param>
    /// <returns>The raw-bound property identities.</returns>
    internal static IReadOnlySet<SemanticId> Raw(SemanticApplication application, SemanticCommand command)
    {
        var marked = Marked(application, command);

        return command.Properties.Where(property => marked.Contains(property.Id) && property.Type.Kind == SemanticTypeReferenceKind.Concept)
            .Select(property => property.Id).ToHashSet();
    }

    static IEnumerable<ArtifactRenderDiagnostic> Verify(SemanticApplicationContext context, SemanticCommand command)
    {
        var marked = Marked(context.Application, command);
        foreach (var property in command.Properties.Where(property => marked.Contains(property.Id)))
        {
            if (Identifiers.ToCamelCase(property.Name) != property.Name)
            {
                yield return Refusal(command, property, "Arc names the failing member in camel case, so its refusal message would not match the reference evaluator's");
            }
            else if (property.Type is { Kind: SemanticTypeReferenceKind.Concept, IsOptional: true })
            {
                // An absent DateTimeOffset has no conversion to an absent concept; Arc#3350 would let the concept bind directly.
                yield return Refusal(command, property, $"optional concept '{context.Concepts[property.Type.Target].Name}' cannot bind as a DateTimeOffset with Arc's explicit-offset marker and convert back to the concept (Arc#3350)");
            }
            else if (property.Type.Kind == SemanticTypeReferenceKind.Concept && Unbindable(context.Concepts[property.Type.Target]) is { } reason)
            {
                yield return Refusal(command, property, $"concept '{context.Concepts[property.Type.Target].Name}' {reason}, so it cannot bind as a DateTimeOffset with Arc's explicit-offset marker");
            }
        }
    }

    // Compliance travels with the concept type, which a plain DateTimeOffset property would drop. A date concept cannot carry
    // validation rules or named values, because the binder admits only comparison rules on commands, so nothing else is checked.
    static string? Unbindable(SemanticConcept concept) => concept.Compliance is not null ? "carries compliance" : null;

    static ArtifactRenderDiagnostic Refusal(SemanticCommand command, SemanticProperty property, string reason) => new(
        SemanticVersionFeatures.DateOrdering,
        ArtifactRenderDiagnosticSeverity.Error,
        $"Command '{command.Name}' compares DateTime property '{property.Name}'; {reason}.",
        command.Id);
}
