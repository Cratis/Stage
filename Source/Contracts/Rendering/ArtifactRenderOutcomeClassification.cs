// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

static class ArtifactRenderOutcomeClassification
{
    internal static ArtifactRenderOutcome Aggregate(IEnumerable<ArtifactRenderOutcome> outcomes)
    {
        var result = ArtifactRenderOutcome.Rendered;
        foreach (var outcome in outcomes)
        {
            if (outcome == ArtifactRenderOutcome.Refused) return ArtifactRenderOutcome.Refused;
            if (outcome == ArtifactRenderOutcome.Degraded) result = ArtifactRenderOutcome.Degraded;
        }
        return result;
    }

    internal static ArtifactRenderOutcome Classify(ArtifactRenderConstruct construct) => Classify(
        [.. construct.Degradations.Select(gap => gap.Evidence.Diagnostic), .. construct.RefusalReasons],
        [.. construct.Degradations.Select(gap => gap.Evidence)]);

    internal static ArtifactRenderOutcome Classify(ImmutableArray<ArtifactRenderDiagnostic> generationDiagnostics, ImmutableArray<ArtifactRenderDegradation> degradations)
    {
        var evidence = ArtifactRenderDegradation.Normalize(degradations, generationDiagnostics).Select(gap => gap.Diagnostic).ToHashSet();
        return Aggregate(generationDiagnostics.Select(diagnostic => diagnostic.Severity switch
        {
            ArtifactRenderDiagnosticSeverity.Error => ArtifactRenderOutcome.Refused,
            ArtifactRenderDiagnosticSeverity.Warning when evidence.Contains(diagnostic) => ArtifactRenderOutcome.Degraded,
            _ => ArtifactRenderOutcome.Rendered
        }));
    }
}
