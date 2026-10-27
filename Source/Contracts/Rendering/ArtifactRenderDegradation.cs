// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Records a root degradation using its original warning, before acceptance promotion.</summary>
/// <param name="Diagnostic">The emitted warning identifying the degraded semantic construct.</param>
/// <param name="Remedy">The action required to resolve the gap.</param>
public sealed record ArtifactRenderDegradation(ArtifactRenderDiagnostic Diagnostic, string Remedy)
{
    static readonly ImmutableHashSet<string> _codes = ImmutableHashSet.Create(StringComparer.Ordinal, ArtifactRenderDegradationCodes.UnknownOwnerMember, ArtifactRenderDegradationCodes.UnsupportedConstruct, ArtifactRenderDegradationCodes.InlineMismatch, ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange, ArtifactRenderDegradationCodes.DependencyDegradation);

    internal static ImmutableArray<ArtifactRenderDegradation> Normalize(ImmutableArray<ArtifactRenderDegradation> gaps, ImmutableArray<ArtifactRenderDiagnostic> diagnostics)
    {
        if (gaps.IsDefault) throw new InvalidArtifactRenderContract("Degradation metadata must be initialized.");
        foreach (var gap in gaps) Validate(gap, diagnostics);
        if (gaps.Select(gap => gap.Diagnostic).Distinct().Count() != gaps.Length)
            throw new InvalidArtifactRenderContract("Degradation metadata contains duplicate warning evidence.");
        return [.. gaps.OrderBy(gap => gap.Diagnostic.Artifact.ToString(), StringComparer.Ordinal)
            .ThenBy(gap => gap.Diagnostic.Code, StringComparer.Ordinal).ThenBy(gap => gap.Diagnostic.Message, StringComparer.Ordinal)];
    }

    static void Validate(ArtifactRenderDegradation gap, ImmutableArray<ArtifactRenderDiagnostic> diagnostics)
    {
        if (gap?.Diagnostic is not { } diagnostic) throw new InvalidArtifactRenderContract("A degradation requires an original warning.");
        if (diagnostic.Artifact is not { IsSet: true }) throw new InvalidArtifactRenderContract("A degradation requires a set semantic identity.");
        if (diagnostic.Severity != ArtifactRenderDiagnosticSeverity.Warning) throw new InvalidArtifactRenderContract("Only original warnings can be promoted by strict acceptance.");
        if (!_codes.Contains(diagnostic.Code)) throw new InvalidArtifactRenderContract("A degradation requires a public degradation code.");
        if (!diagnostics.Contains(diagnostic)) throw new InvalidArtifactRenderContract("Degradation evidence does not match an emitted warning.");
        if (string.IsNullOrWhiteSpace(gap.Remedy)) throw new InvalidArtifactRenderContract("A degradation requires an actionable remedy.");
    }
}
