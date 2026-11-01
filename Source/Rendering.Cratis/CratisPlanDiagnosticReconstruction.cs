// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

static class CratisPlanDiagnosticReconstruction
{
    internal static ArtifactRenderPlan Append(ArtifactRenderRequest request, ArtifactRenderPlan plan, ImmutableArray<ArtifactRenderDiagnostic> diagnostics)
    {
        var original = plan.Strict ? plan.WithAcceptance(plan.Degradations) : plan;
        ImmutableArray<ArtifactRenderDiagnostic> generation = [.. original.Diagnostics, .. diagnostics];
        var removals = generation.Any(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error) ? [] : plan.RequiredRemovals;
        var rebuilt = ArtifactRenderPlan.Create(request with { Scope = plan.Scope, AdditionalScopes = plan.AdditionalScopes }, plan.Artifacts, generation, plan.ImplementationGaps, plan.Refusals, removals);
        rebuilt = rebuilt.WithInlineCodeFingerprint(plan.InlineCodeFingerprint);
        if (plan.Inventory is not null) rebuilt = rebuilt.WithInventory(plan.Inventory);
        if (!plan.PlanEvidence.IsEmpty) rebuilt = rebuilt.WithPlanEvidence(plan.PlanEvidence);
        return plan.Strict || !plan.Degradations.IsEmpty ? rebuilt.WithAcceptance(plan.Degradations, plan.Strict) : rebuilt;
    }
}
