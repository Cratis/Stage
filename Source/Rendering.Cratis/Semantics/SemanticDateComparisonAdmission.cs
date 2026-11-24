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
/// authorization, as the reference evaluator refuses it. Arc applies the marker to primitive and concept-wrapped dates,
/// so compared concept properties retain their nominal type, optionality, validation and compliance.
/// Arc names the failing member in camel case, so a property whose declared name is not camel case is refused.
/// </remarks>
internal static class SemanticDateComparisonAdmission
{
    internal static IEnumerable<ArtifactRenderDiagnostic> Verify(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices) =>
        slices.SelectMany(located => located.Slice.Commands).SelectMany(command => Verify(context.Application, command));

    /// <summary>
    /// Finds the compared DateTime command properties, which carry Arc's explicit-offset marker.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <param name="command">The command.</param>
    /// <returns>The marked property identities.</returns>
    internal static IReadOnlySet<SemanticId> Marked(SemanticApplication application, SemanticCommand command) =>
        SemanticDateComparisons.Participants(application, command).Where(participant => participant.Value == SemanticPrimitiveType.DateTime)
            .Select(participant => participant.Key).ToHashSet();

    static IEnumerable<ArtifactRenderDiagnostic> Verify(SemanticApplication application, SemanticCommand command)
    {
        var marked = Marked(application, command);
        foreach (var property in command.Properties.Where(property => marked.Contains(property.Id)))
        {
            if (Identifiers.ToCamelCase(property.Name) != property.Name)
            {
                yield return Refusal(command, property, "Arc names the failing member in camel case, so its refusal message would not match the reference evaluator's");
            }
        }
    }

    static ArtifactRenderDiagnostic Refusal(SemanticCommand command, SemanticProperty property, string reason) => new(
        SemanticVersionFeatures.DateOrdering,
        ArtifactRenderDiagnosticSeverity.Error,
        $"Command '{command.Name}' compares DateTime property '{property.Name}'; {reason}.",
        command.Id);
}
