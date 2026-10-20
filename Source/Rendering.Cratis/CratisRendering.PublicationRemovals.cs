// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

public static partial class CratisRendering
{
    static CratisPublicationCheck.UnsafeRemovals? CheckRemovals(ArtifactRenderPlan plan, Func<string, string?> readExistingFile)
    {
        var unsafeRemovals = new List<ArtifactRemoval>();
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        foreach (var removal in plan.RequiredRemovals)
        {
            // An already absent file is compatible, so recovering an interrupted publication stays idempotent.
            var existing = readExistingFile(removal.RelativePath);
            var actual = existing is null ? removal.ExpectedSha256 : Sha256Of(removal.RelativePath, existing);
            if (actual != removal.ExpectedSha256)
            {
                unsafeRemovals.Add(removal with { ExpectedSha256 = actual });
                diagnostics.Add(RemovalError($"'{removal.RelativePath}' changed since migration evidence was taken; refusing to remove it.", removal));
            }
        }

        var removed = plan.RequiredRemovals.Select(removal => removal.RelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (readModel, pinned) in PublishedOverrides(plan, readExistingFile).OrderBy(entry => entry.Value.PreviousPath, StringComparer.Ordinal))
        {
            if (!removed.Contains(pinned.PreviousPath) && readExistingFile(pinned.PreviousPath) is { } leftover)
            {
                var removal = new ArtifactRemoval(pinned.PreviousPath, Sha256Of(pinned.PreviousPath, leftover), readModel);
                unsafeRemovals.Add(removal);
                diagnostics.Add(RemovalError($"'{pinned.PreviousPath}' still holds the classic reducer of a migrated read model and the plan does not remove it; publishing would register two builders.", removal));
            }
        }

        return unsafeRemovals.Count == 0
            ? null
            : new([.. unsafeRemovals.OrderBy(removal => removal.RelativePath, StringComparer.Ordinal)], [.. diagnostics]);
    }

    static ImmutableDictionary<SemanticId, ReducerOverride> PublishedOverrides(ArtifactRenderPlan plan, Func<string, string?> readExistingFile)
    {
        // An application plan carries the configuration it persists; a scoped plan that writes reducers publishes beside the destination's.
        var planned = plan.Artifacts.SingleOrDefault(artifact => artifact.RelativePath == ReducerConventionInput.RelativePath);
        var json = planned is not null ? Encoding.UTF8.GetString(planned.Bytes.AsSpan()) : DestinationConvention(plan, readExistingFile);
        return json is not null && ReducerConventionInput.TryParse(json, plan.ApplicationName, out var convention)
            ? convention.Overrides
            : [];
    }

    static string? DestinationConvention(ArtifactRenderPlan plan, Func<string, string?> readExistingFile) =>
        plan.Scope.Kind != ArtifactRenderScopeKind.Application && WritesReducers(plan) ? readExistingFile(ReducerConventionInput.RelativePath) : null;

    static bool WritesReducers(ArtifactRenderPlan plan) =>
        plan.Artifacts.Any(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("Cratis.Chronicle.Reducers.", StringComparison.Ordinal));

    static string Sha256Of(string relativePath, string content) => PlannedArtifact.CreateText(relativePath, content).Sha256;

    static ArtifactRenderDiagnostic RemovalError(string message, ArtifactRemoval removal) =>
        new("STAGE-REDUCER-005", ArtifactRenderDiagnosticSeverity.Error, message, removal.Model);
}
