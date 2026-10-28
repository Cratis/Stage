// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Turns rendered files and their gaps into the artifacts, warnings, inventory and degradation evidence of a plan.
/// </summary>
static class DeclarativePlanAssembly
{
    internal static ArtifactRenderPlan Assemble(ArtifactRenderRequest request, ImmutableArray<DeclarativeRenderedFile> files, ImmutableArray<ArtifactRenderComponent> components)
    {
        var selected = DeclarativeConstruct.Select(request).DistinctBy(_ => _.Id).ToDictionary(_ => _.Id);
        var gaps = files.SelectMany(file => file.Gaps.Select(gap => Warning(request, file, gap, selected))).Distinct().ToImmutableArray();
        var touched = files.Where(_ => _.Item is not null).Select(_ => _.Item!.Value).Concat(gaps.Where(_ => _.Construct is not null).Select(_ => _.Construct!.Id)).ToHashSet();
        var omitted = selected.Values.Where(_ => !touched.Contains(_.Id)).Select(Omitted).ToImmutableArray();
        var all = gaps.Concat(omitted).ToImmutableArray();
        var artifacts = files.Select(Artifact).ToImmutableArray();
        var plan = ArtifactRenderPlan.Create(request, artifacts, [.. all.Select(_ => _.Warning)]);
        plan = plan.WithInventory(Inventory(selected.Values, all));
        var unowned = all.Where(_ => _.Construct is null).Select(_ => _.Degradation).ToImmutableArray();
        if (!unowned.IsEmpty)
        {
            plan = plan.WithPlanEvidence(unowned);
        }

        return plan.WithInputComponents(components);
    }

    static ArtifactRenderInventory Inventory(IEnumerable<DeclarativeConstruct> selected, ImmutableArray<Gap> gaps) =>
        ArtifactRenderInventory.Create([.. selected.Select(construct => Construct(construct, [.. gaps.Where(gap => gap.Construct?.Id == construct.Id)]))]);

    static ArtifactRenderConstruct Construct(DeclarativeConstruct construct, ImmutableArray<Gap> gaps) =>
        new(construct.Id, construct.Kind, null, gaps.IsEmpty ? ArtifactRenderOutcome.Rendered : ArtifactRenderOutcome.Degraded)
        {
            SemanticLocator = construct.Address,
            Degradations = [.. gaps.Select(gap => new ArtifactRenderConstructGap(gap.Degradation, gap.Placeholder, gap.Intent, null) { SemanticLocator = construct.Address })]
        };

    static PlannedArtifact Artifact(DeclarativeRenderedFile file) =>
        PlannedArtifact.CreateVerbatimText(file.Path, [.. new UTF8Encoding(false).GetBytes(file.Text)], file.Item is { } item ? [item] : []);

    static Gap Warning(ArtifactRenderRequest request, DeclarativeRenderedFile file, DeclarativeOwnedGap gap, Dictionary<SemanticId, DeclarativeConstruct> selected)
    {
        var construct = gap.Owner is { } owner && selected.TryGetValue(owner, out var found) ? found : null;
        var message = $"{gap.Gap.Intent} ({file.Path}: {gap.Gap.Template}:{gap.Gap.Line})";
        var warning = new ArtifactRenderDiagnostic(gap.Gap.Code, ArtifactRenderDiagnosticSeverity.Warning, message, construct?.Id ?? request.Model.Application.Id);
        return new(warning, new(warning, $"Implement the construct by hand or extend the target kit: {gap.Gap.Intent}"), construct, gap.Placeholder, gap.Gap.Intent);
    }

    static Gap Omitted(DeclarativeConstruct construct)
    {
        var intent = $"The kit renders no output for {construct.Kind.ToString().ToLowerInvariant()} '{construct.Name}'.";
        var warning = new ArtifactRenderDiagnostic(ArtifactRenderDegradationCodes.UnsupportedConstruct, ArtifactRenderDiagnosticSeverity.Warning, intent, construct.Id);
        return new(warning, new(warning, $"Implement the construct by hand or extend the target kit: {intent}"), construct, construct.Placeholder, intent);
    }

    sealed record Gap(ArtifactRenderDiagnostic Warning, ArtifactRenderDegradation Degradation, DeclarativeConstruct? Construct, ArtifactRenderPlaceholderKind Placeholder, string Intent);
}
