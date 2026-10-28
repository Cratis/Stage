// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Turns rendered files and their gaps into the artifacts, warnings and degradation evidence of a plan.
/// </summary>
static class DeclarativePlanAssembly
{
    internal static ArtifactRenderPlan Assemble(ArtifactRenderRequest request, ImmutableArray<DeclarativeRenderedFile> files, ImmutableArray<ArtifactRenderComponent> components)
    {
        var artifacts = files.Select(Artifact).ToImmutableArray();
        var warnings = files.SelectMany(file => file.Gaps.Select(gap => Warning(request, file, gap))).Distinct().ToImmutableArray();
        var plan = ArtifactRenderPlan.Create(request, artifacts, [.. warnings.Select(_ => _.Warning)]);
        var owned = warnings.Where(_ => _.Owned).Select(_ => _.Degradation).ToImmutableArray();
        var unowned = warnings.Where(_ => !_.Owned).Select(_ => _.Degradation).ToImmutableArray();
        if (!owned.IsEmpty)
        {
            plan = plan.WithAcceptance(owned);
        }

        if (!unowned.IsEmpty)
        {
            plan = plan.WithPlanEvidence(unowned);
        }

        return plan.WithInputComponents(components);
    }

    static PlannedArtifact Artifact(DeclarativeRenderedFile file) =>
        PlannedArtifact.CreateVerbatimText(file.Path, [.. new UTF8Encoding(false).GetBytes(file.Text)], file.Item is { } item ? [item] : []);

    static Gap Warning(ArtifactRenderRequest request, DeclarativeRenderedFile file, DeclarativeOwnedGap gap)
    {
        var owner = gap.Owner ?? request.Model.Application.Id;
        var message = $"{gap.Gap.Intent} ({file.Path}: {gap.Gap.Template}:{gap.Gap.Line})";
        var warning = new ArtifactRenderDiagnostic(gap.Gap.Code, ArtifactRenderDiagnosticSeverity.Warning, message, owner);
        return new(warning, new(warning, $"Implement the construct by hand or extend the target kit: {gap.Gap.Intent}"), gap.Owner is not null);
    }

    sealed record Gap(ArtifactRenderDiagnostic Warning, ArtifactRenderDegradation Degradation, bool Owned);
}
