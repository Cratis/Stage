// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Defines artifact-plan diagnostic severity.
/// </summary>
public enum ArtifactRenderDiagnosticSeverity
{
    /// <summary>
    /// An unknown severity. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Informational target or artifact detail.
    /// </summary>
    Information = 0,

    /// <summary>
    /// A non-blocking render concern.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// A blocking render failure.
    /// </summary>
    Error = 2
}

/// <summary>
/// Defines the content carried by a planned artifact.
/// </summary>
public enum PlannedArtifactKind
{
    /// <summary>
    /// An unknown kind. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// UTF-8 text normalized to LF without a byte-order mark.
    /// </summary>
    Text = 0,

    /// <summary>
    /// Opaque binary bytes.
    /// </summary>
    Binary = 1
}

/// <summary>
/// Represents one typed artifact-planning diagnostic.
/// </summary>
/// <param name="Code">The stable target-owned diagnostic code.</param>
/// <param name="Severity">The severity.</param>
/// <param name="Message">Human-readable details.</param>
/// <param name="Artifact">The related semantic artifact, or a default identity for an application-wide diagnostic.</param>
public sealed record ArtifactRenderDiagnostic(
    string Code,
    ArtifactRenderDiagnosticSeverity Severity,
    string Message,
    SemanticId Artifact);

/// <summary>
/// Represents one immutable normalized target artifact.
/// </summary>
public sealed class PlannedArtifact
{
    PlannedArtifact(PlannedArtifactKind kind, string relativePath, ImmutableArray<byte> bytes, string sha256)
    {
        Kind = kind;
        RelativePath = relativePath;
        Bytes = bytes;
        Sha256 = sha256;
    }

    /// <summary>
    /// Gets the content kind.
    /// </summary>
    public PlannedArtifactKind Kind { get; }

    /// <summary>
    /// Gets the normalized slash-separated relative path.
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// Gets the exact artifact bytes.
    /// </summary>
    public ImmutableArray<byte> Bytes { get; }

    /// <summary>
    /// Gets the lowercase SHA-256 of <see cref="Bytes"/>.
    /// </summary>
    public string Sha256 { get; }

    /// <summary>
    /// Creates, normalizes, encodes, and hashes one text artifact.
    /// </summary>
    /// <param name="relativePath">The portable relative path.</param>
    /// <param name="content">The text content.</param>
    /// <returns>The planned artifact.</returns>
    public static PlannedArtifact CreateText(string relativePath, string content)
    {
        if (content is null)
        {
            throw new InvalidArtifactRenderContract("Artifact text cannot be null.");
        }

        var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Create(PlannedArtifactKind.Text, relativePath, [.. new UTF8Encoding(false).GetBytes(normalized)]);
    }

    /// <summary>
    /// Creates, normalizes, and hashes one binary artifact.
    /// </summary>
    /// <param name="relativePath">The portable relative path.</param>
    /// <param name="bytes">The exact bytes.</param>
    /// <returns>The planned artifact.</returns>
    public static PlannedArtifact CreateBinary(string relativePath, ImmutableArray<byte> bytes) =>
        Create(PlannedArtifactKind.Binary, relativePath, bytes);

    internal static PlannedArtifact Create(PlannedArtifactKind kind, string relativePath, ImmutableArray<byte> bytes)
    {
        var normalized = NormalizePath(relativePath);
        if (!Enum.IsDefined(kind) || kind == PlannedArtifactKind.Unknown || bytes.IsDefault)
        {
            throw new InvalidArtifactRenderContract($"Artifact '{normalized}' kind or bytes are malformed.");
        }

        var hash = Convert.ToHexString(SHA256.HashData(bytes.AsSpan())).ToLowerInvariant();
        return new(kind, normalized, bytes, hash);
    }

    internal static string NormalizePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidArtifactRenderContract("An artifact path cannot be empty.");
        }

        var normalized = relativePath.Replace('\\', '/');
        if (normalized.StartsWith('/') || (normalized.Length >= 2 && char.IsAsciiLetter(normalized[0]) && normalized[1] == ':'))
        {
            throw new InvalidArtifactRenderContract($"Artifact path '{relativePath}' must be relative.");
        }

        var segments = normalized.Split('/');
        if (segments.Any(_ => string.IsNullOrEmpty(_) || string.Equals(_, ".", StringComparison.Ordinal) || string.Equals(_, "..", StringComparison.Ordinal)))
        {
            throw new InvalidArtifactRenderContract($"Artifact path '{relativePath}' contains empty, current-directory, or traversal segments.");
        }

        return normalized;
    }
}

/// <summary>
/// Represents a complete deterministic artifact plan before publication.
/// </summary>
public sealed class ArtifactRenderPlan
{
    ArtifactRenderPlan(
        string target,
        string targetVersion,
        string renderer,
        string rendererVersion,
        string applicationName,
        SemanticRevision semanticRevision,
        ImmutableArray<PlannedArtifact> artifacts,
        ImmutableArray<ArtifactRenderDiagnostic> diagnostics)
    {
        Target = target;
        TargetVersion = targetVersion;
        Renderer = renderer;
        RendererVersion = rendererVersion;
        ApplicationName = applicationName;
        SemanticRevision = semanticRevision;
        Artifacts = artifacts;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the target identity.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the exact target profile version.
    /// </summary>
    public string TargetVersion { get; }

    /// <summary>
    /// Gets the renderer identity.
    /// </summary>
    public string Renderer { get; }

    /// <summary>
    /// Gets the exact renderer version.
    /// </summary>
    public string RendererVersion { get; }

    /// <summary>
    /// Gets the application name independent of publication destination.
    /// </summary>
    public string ApplicationName { get; }

    /// <summary>
    /// Gets the exact semantic revision rendered.
    /// </summary>
    public SemanticRevision SemanticRevision { get; }

    /// <summary>
    /// Gets artifacts in ordinal relative-path order.
    /// </summary>
    public ImmutableArray<PlannedArtifact> Artifacts { get; }

    /// <summary>
    /// Gets diagnostics in deterministic input order.
    /// </summary>
    public ImmutableArray<ArtifactRenderDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets a value indicating whether the plan is complete and publishable.
    /// </summary>
    public bool Success => Diagnostics.All(_ => _.Severity != ArtifactRenderDiagnosticSeverity.Error);

    /// <summary>
    /// Creates and validates one destination-independent artifact plan.
    /// </summary>
    /// <param name="request">The fully resolved render request.</param>
    /// <param name="artifacts">The target artifacts.</param>
    /// <param name="diagnostics">The typed diagnostics.</param>
    /// <returns>The complete deterministic plan.</returns>
    public static ArtifactRenderPlan Create(
        ArtifactRenderRequest request,
        ImmutableArray<PlannedArtifact> artifacts,
        ImmutableArray<ArtifactRenderDiagnostic> diagnostics)
    {
        if (request is null || request.Model is null || request.ExecutionPlan is null || request.Profile is null || request.Scope is null ||
            request.ExecutionPlan.Revision != request.Model.Revision || !ScopeMatches(request) ||
            artifacts.IsDefault || diagnostics.IsDefault || artifacts.Any(_ => _ is null) ||
            diagnostics.Any(_ => _ is null || string.IsNullOrWhiteSpace(_.Code) || string.IsNullOrWhiteSpace(_.Message) ||
                !Enum.IsDefined(_.Severity) || _.Severity == ArtifactRenderDiagnosticSeverity.Unknown))
        {
            throw new InvalidArtifactRenderContract("An artifact render plan contains malformed request, artifact, or diagnostic data.");
        }

        var normalized = artifacts
            .Select(artifact => PlannedArtifact.Create(artifact.Kind, artifact.RelativePath, artifact.Bytes))
            .OrderBy(_ => _.RelativePath, StringComparer.Ordinal)
            .ToImmutableArray();
        if (normalized.Select(_ => _.RelativePath).Distinct(StringComparer.Ordinal).Count() != normalized.Length)
        {
            throw new InvalidArtifactRenderContract("An artifact render plan contains a duplicate relative path.");
        }

        if (normalized.Select(_ => _.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Length)
        {
            throw new InvalidArtifactRenderContract("An artifact render plan contains paths that collide on a case-insensitive file system.");
        }

        return new(
            request.Profile.Target,
            request.Profile.TargetVersion,
            request.Profile.Renderer,
            request.Profile.RendererVersion,
            request.Model.Application.Name,
            request.Model.Revision,
            normalized,
            [
                .. diagnostics
                    .OrderBy(_ => _.Severity)
                    .ThenBy(_ => _.Code, StringComparer.Ordinal)
                    .ThenBy(_ => _.Artifact.ToString(), StringComparer.Ordinal)
                    .ThenBy(_ => _.Message, StringComparer.Ordinal)
            ]);
    }

    static bool ScopeMatches(ArtifactRenderRequest request)
    {
        if (!Enum.IsDefined(request.Scope.Kind) || request.Scope.Kind == ArtifactRenderScopeKind.Unknown || !request.Scope.Artifact.IsSet)
        {
            return false;
        }

        var application = request.Model.Application;
        if (request.Scope.Kind == ArtifactRenderScopeKind.Application)
        {
            return request.Scope.Artifact == application.Id;
        }

        var modules = application.Modules;
        if (request.Scope.Kind == ArtifactRenderScopeKind.Module)
        {
            return modules.Any(_ => _.Id == request.Scope.Artifact);
        }

        var features = AllFeatures(modules.SelectMany(_ => _.Features)).ToArray();
        return request.Scope.Kind switch
        {
            ArtifactRenderScopeKind.Feature => features.Any(_ => _.Id == request.Scope.Artifact),
            ArtifactRenderScopeKind.Slice => features.SelectMany(_ => _.Slices).Any(_ => _.Id == request.Scope.Artifact),
            _ => false
        };
    }

    static IEnumerable<SemanticFeature> AllFeatures(IEnumerable<SemanticFeature> features) =>
        features.SelectMany(_ => new[] { _ }.Concat(AllFeatures(_.Features)));
}
