// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Managed canonical report artifacts. No plan hash is embedded, so adding reports cannot recurse into their bytes.</summary>
/// <param name="Completeness">The canonical JSON report at .stage/completeness.json.</param>
/// <param name="Unsupported">The canonical human report at UNSUPPORTED.md.</param>
public sealed record ArtifactRenderReports(PlannedArtifact Completeness, PlannedArtifact Unsupported)
{
    /// <summary>The reversible prerelease JSON byte/schema version. UTF-8, no BOM, LF, one terminal newline.</summary>
    public const string SchemaVersion = "stage-render-completeness/v1";

    /// <summary>The additive JSON schema version written only for reports carrying a declared range.</summary>
    public const string RangeSchemaVersion = "stage-render-completeness/v2";

    /// <summary>Creates report artifacts from validated metadata; does not discover leaves, admit versions or publish output.</summary>
    /// <param name="plan">A plan with a validated inventory.</param>
    /// <param name="context">The actual input and provider's effective audited provenance.</param>
    /// <returns>Two deterministic managed artifacts with selected-scope source ownership.</returns>
    public static ArtifactRenderReports Create(ArtifactRenderPlan plan, ArtifactRenderReportContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Inventory is null) throw new InvalidArtifactRenderContract("Completeness reports require a selected semantic inventory.");
        if (context is null) throw new InvalidArtifactRenderContract("Completeness reports require typed provider provenance.");
        var normalized = context.Normalize();
        if (normalized.Input != plan.InputVersions) throw new InvalidArtifactRenderContract("Report versions do not match the original input model.");
        var sources = Scopes(plan).Select(scope => scope.Artifact).Distinct().ToImmutableArray();
        return new(
            PlannedArtifact.CreateText(".stage/completeness.json", System.Text.Encoding.UTF8.GetString(ArtifactRenderReportWriter.Bytes(plan, normalized)), sources),
            PlannedArtifact.CreateText("UNSUPPORTED.md", ArtifactRenderMarkdown.Write(plan), sources));
    }

    internal static IEnumerable<ArtifactRenderScope> Scopes(ArtifactRenderPlan plan) => new[] { plan.Scope }.Concat(plan.AdditionalScopes)
        .Distinct().OrderBy(scope => scope.Kind.ToString(), StringComparer.Ordinal).ThenBy(scope => scope.Artifact.ToString(), StringComparer.Ordinal);
}
