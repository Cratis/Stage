// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

static class RenderingGapReports
{
    internal static void AddMarker(List<PlannedArtifact> artifacts, ImmutableArray<ArtifactImplementationGap> gaps, SemanticId application)
    {
        artifacts.RemoveAll(artifact => artifact.RelativePath == "STAGE-NOT-RUNNABLE.md");
        var entries = gaps.OrderBy(gap => gap.RequirementId, StringComparer.Ordinal)
            .Select(gap => $"- {gap.Kind} gap `{gap.RequirementId}` ({gap.Owner}): {string.Join(' ', gap.Hints)}");
        artifacts.Add(PlannedArtifact.CreateText(
            "STAGE-NOT-RUNNABLE.md",
            $"# Not fully runnable\n\nThis render contains implementation gaps. Unsupported owners are omitted or represented by throwing stubs.\n\n{string.Join('\n', entries)}\n",
            [application]));
    }

    internal static bool IsPathDiagnostic(ArtifactRenderDiagnostic diagnostic) =>
        string.Equals(diagnostic.Code, "STAGE-CRATIS-004", StringComparison.Ordinal) || string.Equals(diagnostic.Code, "STAGE-CRATIS-005", StringComparison.Ordinal);

    internal static ImmutableArray<ArtifactImplementationGap> FromPathDiagnostics(IEnumerable<ArtifactRenderDiagnostic> diagnostics) =>
        FromDiagnostics(diagnostics.Where(IsPathDiagnostic));

    internal static ImmutableArray<ArtifactImplementationGap> FromDiagnostics(IEnumerable<ArtifactRenderDiagnostic> diagnostics) =>
        [.. diagnostics.GroupBy(diagnostic => (diagnostic.Code, diagnostic.Artifact))
            .Select(group => new ArtifactImplementationGap(
                ArtifactImplementationGapOrigin.Unverified,
                $"render:{group.Key.Code}:{group.Key.Artifact}",
                group.Key.Artifact.ToString(),
                SemanticImplementationRole.CommandHandler,
                [.. group.Select(diagnostic => diagnostic.Message).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
                1,
                1,
                []) { Kind = ArtifactImplementationGapKind.Rendering })];
}
