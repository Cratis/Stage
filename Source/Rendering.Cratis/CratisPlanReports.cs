// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

static class CratisPlanReports
{
    internal static ArtifactRenderPlan Finalize(ArtifactRenderRequest request, ArtifactRenderPlan plan, CratisRenderingOptions options)
    {
        if (request.IdentityCatalog is null && request.SemanticIdentities.IsDefaultOrEmpty) return plan;
        if (!plan.CanPublishArtifacts) return plan;
        plan = plan.WithInventory(CratisSemanticInventory.Create(request, plan, options));
        var context = new ArtifactRenderReportContext(plan.InputVersions!, new([plan.InputVersions!]), [new(request.Profile.Renderer, request.Profile.RendererVersion)]);
        var reports = ArtifactRenderReports.Create(plan, context);
        var original = plan.WithAcceptance(plan.Degradations);
        return ArtifactRenderPlan.Create(request, [.. plan.Artifacts, reports.Completeness, reports.Unsupported], original.Diagnostics, plan.ImplementationGaps, plan.Refusals, plan.RequiredRemovals)
            .WithInventory(plan.Inventory!).WithAcceptance(plan.Degradations, plan.Strict);
    }
}
