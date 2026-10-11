// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Refuses each ESM v5–v9 construct the Cratis planner does not render yet, before any artifact is planned.
/// </summary>
/// <remarks>
/// Admitting a version by number never admits its constructs: a selected slice that uses one refuses the
/// whole plan with a diagnostic naming the construct, rather than rendering without it. A model that uses
/// none of them renders exactly as an earlier version would.
/// </remarks>
internal static class SemanticVersionFeatureAdmission
{
    /// <summary>
    /// Finds every unrendered v5–v9 construct in the selected scope.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="slices">The selected slices.</param>
    /// <returns>One blocking diagnostic per construct, in model order.</returns>
    public static ImmutableArray<ArtifactRenderDiagnostic> Verify(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var features = slices.Select(located => located.Slice).SelectMany(slice =>
            SemanticVersionFeatures.InSlice(slice)
                .Concat(SemanticVersionFeatures.EventReferences(slice, context.Events))
                .Concat(slice.Commands.SelectMany(command => SemanticVersionFeatures.InCommand(command).Except(SemanticVersionFeatures.RoutesInCommand(command))))
                .Concat(slice.Specifications.SelectMany(specification => SemanticVersionFeatures.InSpecification(specification).Except(SemanticVersionFeatures.RoutesInSpecification(specification)))));

        // A scoped render also emits sibling constraints that govern its produced events.
        features = features.Concat(SemanticVersionFeatures.EventReferences(
            SemanticCratisAdmission.SelectedConstraints(context, slices).Select(selected => selected.Constraint), context.Events)).Distinct();

        // Application triggers belong to no slice; only an application render would otherwise drop them.
        if (context.Request.Scope.Kind == ArtifactRenderScopeKind.Application)
        {
            features = SemanticVersionFeatures.InApplication(context.Application).Except(SemanticVersionFeatures.RoutesInApplication(context.Application)).Concat(features);
        }

        return [.. features.Select(feature => new ArtifactRenderDiagnostic(feature.Code, ArtifactRenderDiagnosticSeverity.Error, feature.Message, feature.Artifact))];
    }
}
