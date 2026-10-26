// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

static class CratisPlanAcceptance
{
    internal static ArtifactRenderPlan Apply(ArtifactRenderRequest request, ArtifactRenderPlan plan, CratisRenderingOptions options)
    {
        if (!options.Strict)
        {
            var language = LanguageEvidence(plan);
            return language.IsEmpty ? plan : plan.WithAcceptance([.. plan.Degradations, .. language]);
        }

        var evidence = PerformerEvidence(request, plan, options);
        if (!evidence.IsEmpty)
        {
            plan = CratisPlanDiagnosticReconstruction.Append(request, plan, [.. evidence.Select(gap => gap.Diagnostic)]);
        }
        return plan.WithAcceptance([.. plan.Degradations, .. evidence, .. LanguageEvidence(plan)], strict: true);
    }

    // The warning is already emitted; acceptance only pairs it with the remedy, once per distinct warning.
    static ImmutableArray<ArtifactRenderDegradation> LanguageEvidence(ArtifactRenderPlan plan) =>
        [.. plan.Diagnostics
            .Where(diagnostic => diagnostic.Code == ArtifactRenderDegradationCodes.InlineMismatch && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning)
            .Distinct()
            .Select(diagnostic => new ArtifactRenderDegradation(diagnostic, ImplementationLanguage.Remedy))];

    static ImmutableArray<ArtifactRenderDegradation> PerformerEvidence(ArtifactRenderRequest request, ArtifactRenderPlan plan, CratisRenderingOptions options)
    {
        var gaps = plan.ImplementationGaps.Where(gap => gap.Role == SemanticImplementationRole.QueryPerformer).ToArray();
        if (gaps.Length == 0) return [];
        var queries = new SemanticApplicationContext(request, options).SelectedSlices().SelectMany(slice => slice.Slice.Queries).ToArray();
        return [.. gaps.Select(gap => Evidence(gap, queries))];
    }

    static ArtifactRenderDegradation Evidence(ArtifactImplementationGap gap, SemanticKeyedQuery[] queries)
    {
        var query = queries.SingleOrDefault(query => query.Id.ToString() == gap.Owner)
            ?? throw new InvalidArtifactRenderContract("A performer gap requires its selected semantic query owner.");
        var warning = new ArtifactRenderDiagnostic(
            ArtifactRenderDegradationCodes.UnsupportedConstruct,
            ArtifactRenderDiagnosticSeverity.Warning,
            $"Query '{query.Name}' requirement '{gap.RequirementId}' retains a throwing performer body ({gap.Origin}).",
            query.Id);
        return new(warning, "Supply a supported executable performer implementation and verify the generated query.");
    }
}
