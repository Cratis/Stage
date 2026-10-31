// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class CratisEsmRangeAdmission
{
    internal static CratisEsmRangeVerdict Evaluate(ArtifactRenderRequest request, SemanticApplicationContext context)
    {
        var version = CratisEsmSupport.Descriptor.Classify(request.Model.LanguageVersion, request.Model.SemanticVersion);
        if (version != EsmVersionStanding.NewerWithinRange && !ScansAuditedMembers(request.Model, version)) return FromFindings(request, new(version, []));
        var owners = CratisSelectedSemanticOwners.Enumerate(context).Select(owner => owner.Id).Append(context.Application.Id).ToImmutableHashSet();
        var selected = context.SelectedSlices().Select(located => located.Slice.Id).ToImmutableHashSet();
        var excluded = context.AllSlices.Select(located => located.Slice.Id).Where(id => !selected.Contains(id)).ToImmutableHashSet();
        var scope = EsmRangeSurface.Scope(owners, excluded);
        var standing = version == EsmVersionStanding.Audited
            ? new EsmSupportStanding(version, [.. SemanticUnknownMembers.Find(request.Model, EsmRangeSurface.Known, scope)
                .Where(member => member.Member == $"{nameof(SemanticSpecification)}.{nameof(SemanticSpecification.ThenInvocations)}")])
            : EsmSupportStanding.Evaluate(CratisEsmSupport.Descriptor, request.Model, EsmRangeSurface.Known, scope);
        return FromFindings(request, standing);
    }

    // Only the audited v34 pair can carry 'then invokes' expectations, so older audited pairs skip the member scan entirely.
    internal static bool ScansAuditedMembers(ExecutableSemanticModel model, EsmVersionStanding standing) =>
        standing == EsmVersionStanding.Audited && model.SemanticVersion.Major >= SemanticVersion.V34.Major;

    internal static CratisEsmRangeVerdict FromFindings(ArtifactRenderRequest request, EsmSupportStanding standing)
    {
        if (standing.Version == EsmVersionStanding.Audited) return new(standing, [.. standing.UnknownMembers.Select(Finding)], []);
        var pair = $"{request.Model.LanguageVersion}/{request.Model.SemanticVersion}";
        if (standing.Version != EsmVersionStanding.NewerWithinRange)
        {
            var refused = new ArtifactRenderDiagnostic(
                "STAGE-ESM-016",
                ArtifactRenderDiagnosticSeverity.Error,
                $"ESM pair {pair} is not admitted by the Cratis range >=32.0 <33.",
                request.Model.Application.Id);
            return new(standing, [refused], []);
        }
        return Newer(request, standing, pair);
    }

    static CratisEsmRangeVerdict Newer(ArtifactRenderRequest request, EsmSupportStanding standing, string pair)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        var evidence = ImmutableArray.CreateBuilder<ArtifactRenderDegradation>();
        var newer = new ArtifactRenderDiagnostic(
            ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange,
            ArtifactRenderDiagnosticSeverity.Warning,
            $"ESM pair {pair} is within >=32.0 <33 but has not been audited by this Stage version.",
            request.Model.Application.Id);
        diagnostics.Add(newer);
        evidence.Add(new(newer, "Upgrade Stage to audit this pair, or use an audited ESM pair."));
        foreach (var member in standing.UnknownMembers.OrderBy(member => member.Path, StringComparer.Ordinal).ThenBy(member => member.Member, StringComparer.Ordinal))
        {
            var diagnostic = Finding(member);
            diagnostics.Add(diagnostic);
            if (!member.SecurityRelevant) evidence.Add(new(diagnostic, "Upgrade Stage to audit this member, or remove it from the owner."));
        }
        return new(standing, diagnostics.ToImmutable(), evidence.ToImmutable());
    }

    static ArtifactRenderDiagnostic Finding(SemanticUnknownMember member) => new(
        member.SecurityRelevant ? "STAGE-ESM-016" : ArtifactRenderDegradationCodes.UnknownOwnerMember,
        member.SecurityRelevant ? ArtifactRenderDiagnosticSeverity.Error : ArtifactRenderDiagnosticSeverity.Warning,
        $"Unaudited ESM member {member.Member} at {member.Path}.",
        member.Owner);
}
