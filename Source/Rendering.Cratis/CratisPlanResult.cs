// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Describes an expected source, selection, option, or rendering outcome.
/// </summary>
/// <param name="Code">The stable diagnostic code; compiler PLAY codes are preserved.</param>
/// <param name="Severity">The diagnostic severity.</param>
/// <param name="Message">Human-readable details, including known sibling names for unknown selections.</param>
/// <param name="Artifact">The related semantic identity, or an unset identity.</param>
/// <param name="Source">The root-relative source path and location, if available.</param>
public sealed record CratisPlanDiagnostic(string Code, ArtifactRenderDiagnosticSeverity Severity, string Message, SemanticId Artifact, string? Source);

/// <summary>
/// Carries a deterministic plan or typed expected failures without publishing any files.
/// </summary>
/// <param name="Artifacts">The artifacts in ordinal relative-path order.</param>
/// <param name="Diagnostics">The typed diagnostics in deterministic order.</param>
/// <param name="Digest">The output-only SHA-256 digest, independent of semantic revision.</param>
/// <param name="ApplicationName">The application name.</param>
/// <param name="TargetVersion">The exact Cratis target version.</param>
/// <param name="Revision">The semantic revision, absent for scaffold-only and failed loads.</param>
/// <param name="Plan">The underlying artifact plan, absent for scaffold-only and failures before rendering.</param>
public sealed record CratisPlanResult(
    ImmutableArray<PlannedArtifact> Artifacts,
    ImmutableArray<CratisPlanDiagnostic> Diagnostics,
    string Digest,
    string ApplicationName,
    string TargetVersion,
    SemanticRevision? Revision,
    ArtifactRenderPlan? Plan)
{
    /// <summary>
    /// Gets whether the result has no blocking diagnostics.
    /// </summary>
    public bool Success => Diagnostics.All(diagnostic => diagnostic.Severity != ArtifactRenderDiagnosticSeverity.Error);

    internal static CratisPlanResult Create(string applicationName, ImmutableArray<PlannedArtifact> artifacts, ImmutableArray<CratisPlanDiagnostic> diagnostics, SemanticRevision? revision = null, ArtifactRenderPlan? plan = null)
    {
        var orderedArtifacts = artifacts.OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal).ToImmutableArray();
        var orderedDiagnostics = diagnostics.OrderBy(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Artifact.ToString(), StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source, StringComparer.Ordinal).ToImmutableArray();
        var renderDiagnostics = orderedDiagnostics.Select(diagnostic => new ArtifactRenderDiagnostic(diagnostic.Code, diagnostic.Severity, diagnostic.Source is null ? diagnostic.Message : $"{diagnostic.Source}: {diagnostic.Message}", diagnostic.Artifact)).ToImmutableArray();
        var digest = plan?.Digest ?? ArtifactRenderPlan.ComputeDigest(CratisRendering.TargetId, CratisRendering.TargetVersion, CratisRendering.RendererId, CratisRendering.RendererVersion, applicationName, orderedArtifacts, renderDiagnostics);

        return new(orderedArtifacts, orderedDiagnostics, digest, applicationName, CratisRendering.TargetVersion, revision, plan);
    }

    internal static CratisPlanDiagnostic Error(string code, string message, SemanticId artifact = default, string? source = null) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, artifact, source);
}
