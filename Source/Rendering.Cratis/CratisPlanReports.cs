// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

static class CratisPlanReports
{
    internal static ArtifactRenderPlan Finalize(ArtifactRenderRequest request, ArtifactRenderPlan plan, CratisRenderingOptions options)
    {
        if (request.IdentityCatalog is null && request.SemanticIdentities.IsDefaultOrEmpty) return plan;
        if (!plan.CanPublishArtifacts) return plan;
        if (!plan.ImplementationGaps.IsEmpty && plan.Degradations.IsEmpty) return plan;
        plan = plan.WithInventory(CratisSemanticInventory.Create(request, plan, options));
        var context = new ArtifactRenderReportContext(plan.InputVersions!, Support(plan), [new(request.Profile.Renderer, request.Profile.RendererVersion)]);
        var reports = ArtifactRenderReports.Create(plan, context);
        var placedReports = ServiceLayout.Apply(request.Profile, [reports.Completeness, reports.Unsupported]);
        var original = plan.WithAcceptance(plan.Degradations);
        return ArtifactRenderPlan.Create(request, [.. plan.Artifacts, .. placedReports], original.Diagnostics, plan.ImplementationGaps, plan.Refusals, plan.RequiredRemovals)
            .WithInventory(plan.Inventory!).WithPlanEvidence(plan.PlanEvidence).WithAcceptance(plan.Degradations, plan.Strict);
    }

    // An input inside the declared range gets the additive v2 provenance; every other report keeps the exact v1 shape.
    static ArtifactRenderAuditedSupport Support(ArtifactRenderPlan plan)
    {
        if (!plan.PlanEvidence.Any(gap => gap.Diagnostic.Code == ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange)) return new([plan.InputVersions!]);
        var descriptor = CratisEsmSupport.Descriptor;
        return new([.. descriptor.Audited.Select(pair => new ArtifactRenderVersionPair(pair.Language, pair.Semantics))])
        {
            DeclaredRange = new(new(LanguageVersion.V32, descriptor.Accepts.Minimum), descriptor.Accepts.ExclusiveMajor)
        };
    }
}
