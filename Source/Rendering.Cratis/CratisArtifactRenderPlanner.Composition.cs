// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Realization;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Constraints;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs;
using Cratis.Stage.Rendering.Cratis.Semantics.Lifecycles;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;

namespace Cratis.Stage.Rendering.Cratis;

public sealed partial class CratisArtifactRenderPlanner
{
    static ArtifactRenderPlan CreatePlan(
        ArtifactRenderRequest request,
        IEnumerable<PlannedArtifact> artifacts,
        List<ArtifactRenderDiagnostic> diagnostics,
        PlanOutcome? outcome = null)
    {
        var refusals = outcome?.NormalizedRefusals ?? [];
        var (planned, gaps) = AddPerformerGaps(request, artifacts, diagnostics);

        // Successful diagnostics (and their digests) are unchanged. The contract owns sorting.
        if (HasErrors(diagnostics))
        {
            diagnostics = [.. diagnostics.DistinctBy(diagnostic => (diagnostic.Code, diagnostic.Artifact, diagnostic.Message))];
        }

        var unique = UniqueArtifacts(request, planned, diagnostics);
        if (UsesReservedDirectory(unique))
        {
            diagnostics.Add(Error(
                "STAGE-CRATIS-005",
                "The top-level Customizations directory is reserved for user-owned files. Rename the modeled module or feature that would render into it.",
                request.Model.Application.Id));
            return ArtifactRenderPlan.Create(request, [], [.. diagnostics]);
        }

        if (!refusals.IsEmpty && !TrySummarizeRefusals(request, diagnostics, refusals))
        {
            return ArtifactRenderPlan.Create(request, [], [.. diagnostics]);
        }

        // A plan carrying errors never removes files; publication then refuses the leftover classic reducer.
        var required = HasErrors(diagnostics) ? [] : outcome?.NormalizedRemovals ?? [];

        // The layout is applied last: the checks above and the renderers work on application-relative paths.
        var plan = ArtifactRenderPlan.Create(request, [.. RelocateSourceLines(request.Profile, unique)], [.. diagnostics], gaps, refusals, required);
        return ServiceLayout.Plan(request, plan);
    }

    static bool UsesReservedDirectory(List<PlannedArtifact> unique) =>
        unique.Exists(artifact => artifact.RelativePath.StartsWith("Customizations/", StringComparison.OrdinalIgnoreCase));

    static bool HasErrors(List<ArtifactRenderDiagnostic> diagnostics) => diagnostics.Exists(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);

    static List<PlannedArtifact> UniqueArtifacts(ArtifactRenderRequest request, IEnumerable<PlannedArtifact> planned, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var unique = new List<PlannedArtifact>();
        foreach (var group in planned.GroupBy(_ => _.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            unique.Add(group.OrderBy(_ => _.RelativePath, StringComparer.Ordinal).First());
            if (group.Count() > 1)
            {
                diagnostics.Add(Error(
                    "STAGE-CRATIS-004",
                    $"Generated artifact path '{group.Key}' collides with another scaffold or semantic artifact.",
                    request.Model.Application.Id));
            }
        }

        return unique;
    }

    static bool TrySummarizeRefusals(ArtifactRenderRequest request, List<ArtifactRenderDiagnostic> diagnostics, ImmutableArray<ArtifactRenderRefusal> refusals)
    {
        if (HasUnrefusedError(diagnostics, refusals)) return false;

        AddSpecificationRefusalSummary(request, diagnostics, refusals);
        return true;
    }

    static bool HasUnrefusedError(List<ArtifactRenderDiagnostic> diagnostics, ImmutableArray<ArtifactRenderRefusal> refusals) =>
        diagnostics.Exists(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error && !IsRefused(refusals, diagnostic));

    static bool IsRefused(ImmutableArray<ArtifactRenderRefusal> refusals, ArtifactRenderDiagnostic diagnostic) =>
        refusals.Any(refusal => refusal.Artifact == diagnostic.Artifact && refusal.Codes.Contains(diagnostic.Code));

    // The concepts and types an own public event needs render into the contracts project; the Arc validators of those concepts stay in the application.
    static void AddEventContractCommon(
        SemanticApplicationContext context,
        (IReadOnlyList<SemanticConcept> Concepts, IReadOnlyList<SemanticCompositeType> Types) shared,
        List<PlannedArtifact> artifacts,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        var contracts = context.EventContracts;
        var blockers = contracts.Blockers(context).ToArray();
        foreach (var (@event, concept) in blockers)
        {
            diagnostics.Add(Error(
                SemanticVersionFeatures.PublicEvents,
                $"Public event '{@event.Name}' needs concept '{concept.Name}', which carries Arc metadata (NotAudited) the contracts project cannot reference.",
                @event.Id));
        }

        var open = blockers.Length == 0;
        foreach (var concept in shared.Concepts)
        {
            artifacts.AddRange(ConceptArtifacts(context, concept, open && contracts.ContainsConcept(concept.Id)));
        }

        artifacts.AddRange(shared.Types.Select(type =>
            Artifact(SemanticCommonArtifactRenderer.Render(type, context, placement: open && contracts.ContainsType(type.Id) ? ConceptPlacement.Contracts : ConceptPlacement.Application))));
    }

    static IEnumerable<PlannedArtifact> ConceptArtifacts(SemanticApplicationContext context, SemanticConcept concept, bool inContracts)
    {
        yield return Artifact(SemanticCommonArtifactRenderer.Render(concept, context, placement: inContracts ? ConceptPlacement.Contracts : ConceptPlacement.Application));
        if (inContracts && NeedsValidator(concept))
        {
            yield return Artifact(SemanticCommonArtifactRenderer.Render(concept, context, placement: ConceptPlacement.Validator));
        }
    }

    static bool NeedsValidator(SemanticConcept concept) => concept.Values.IsEmpty && !concept.Validations.IsEmpty;

    sealed record PlanOutcome(ImmutableArray<ArtifactRenderRefusal> Refusals, ImmutableArray<ArtifactRemoval> Removals)
    {
        internal ImmutableArray<ArtifactRenderRefusal> NormalizedRefusals => Refusals.IsDefault ? [] : Refusals;

        internal ImmutableArray<ArtifactRemoval> NormalizedRemovals => Removals.IsDefault ? [] : Removals;
    }
}
