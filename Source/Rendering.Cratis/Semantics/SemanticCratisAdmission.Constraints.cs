// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits only portable constraints whose append-time behavior Chronicle can reproduce.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    internal static IReadOnlyList<(SemanticSlice Slice, SemanticConstraint Constraint)> SelectedConstraints(
        SemanticApplicationContext context,
        IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var selected = slices.Select(_ => _.Slice.Id).ToHashSet();
        var produced = slices.SelectMany(located => located.Slice.Commands).SelectMany(command => command.Produces)
            .Concat(slices.SelectMany(located => located.Slice.Reactions.IsDefault ? [] : located.Slice.Reactions)
                .SelectMany(reaction => reaction.Triggers).SelectMany(SemanticReactionProductions.For))
            .Select(production => production.EventContract).ToHashSet();
        return [.. context.Constraints.Where(_ => selected.Contains(_.Slice.Id) ||
            _.Constraint.Targets.Any(target => produced.Contains(target.EventContract)))];
    }

    static void ValidateConstraints(
        SemanticApplicationContext context,
        IReadOnlyList<LocatedSemanticSlice> slices,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var (slice, constraint) in SelectedConstraints(context, slices))
        {
            if (!GeneratedPascalCase.ConstraintTypeNameIsSafe(constraint.Name))
            {
                diagnostics.Add(Error("STAGE-ESM-012", $"Constraint '{constraint.Name}' has a name that collides with its generated C# members.", slice.Id));
            }

            if (constraint.Kind is not (SemanticConstraintKind.UniquePropertyValue or SemanticConstraintKind.UniqueEventOccurrence) ||
                constraint.Scope != SemanticConstraintScope.EventSequence ||
                !SemanticValidationRendering.SafeMessage(constraint.Message))
            {
                diagnostics.Add(Error("STAGE-ESM-014", $"Constraint '{constraint.Name}' has a kind, scope, or message Chronicle cannot reproduce.", slice.Id));
                continue;
            }

            if (constraint.Kind != SemanticConstraintKind.UniquePropertyValue)
            {
                continue;
            }

            // Chronicle#4123 updates the constraint index after commit. A storage failure can leave an
            // accepted event unindexed; admission cannot make that infrastructure failure atomic.
            foreach (var command in slices.SelectMany(selected => selected.Slice.Commands))
            {
                var affected = constraint.Targets.Select(target => target.EventContract).Concat(constraint.ReleasedBy).ToHashSet();
                if (command.Produces.Count(produced => affected.Contains(produced.EventContract)) > 1)
                {
                    var events = command.Produces.Where(produced => affected.Contains(produced.EventContract)).Select(produced => context.Events[produced.EventContract].Name);
                    diagnostics.Add(Error("STAGE-ESM-014", $"Constraint '{constraint.Name}' cannot preserve changes to multiple claims or releases in one command '{command.Name}' batch ({string.Join(", ", events)}).", command.Id));
                }
            }

            foreach (var target in constraint.Targets)
            {
                var @event = context.Events[target.EventContract];
                var properties = target.Properties.Select(id => @event.Properties.Single(_ => _.Id == id)).ToArray();
                if (properties.Any(_ => _.Type.IsOptional))
                {
                    // Chronicle #4122: null writes an empty-string claim to the index even though validation skips it.
                    var optional = string.Join(", ", properties.Where(property => property.Type.IsOptional).Select(property => property.Name));
                    diagnostics.Add(Error("STAGE-ESM-014", $"Constraint '{constraint.Name}' targets optional properties ({optional}) on '{@event.Name}'; Chronicle #4122 indexes null as an empty-string claim.", slice.Id));
                }

                if (properties.Length != 1)
                {
                    diagnostics.Add(Error("STAGE-ESM-014", $"Constraint '{constraint.Name}' targets composite properties ({string.Join(", ", properties.Select(property => property.Name))}) on '{@event.Name}'; Chronicle's string hash can collide with distinct Screenplay values.", slice.Id));
                }
                else if (properties.Any(property => property.Type.IsCollection || SemanticValidationRendering.UnderlyingPrimitive(property.Type, context) != SemanticPrimitiveType.Text))
                {
                    diagnostics.Add(Error("STAGE-ESM-014", $"Constraint '{constraint.Name}' targets nontext or collection property '{properties[0].Name}' on '{@event.Name}'; Chronicle's string hash can collide with distinct Screenplay values.", slice.Id));
                }
            }
        }
    }
}
