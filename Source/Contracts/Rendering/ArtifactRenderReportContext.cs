// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Typed effective input and provider provenance. Reporting it does not admit a model or authorize publication.</summary>
/// <param name="Input">The input model's actual ESM versions.</param>
/// <param name="Support">The provider's exact audited pairs, with no inferred forward interval.</param>
/// <param name="Packs">The effective package pins.</param>
public sealed record ArtifactRenderReportContext(ArtifactRenderVersionPair Input, ArtifactRenderAuditedSupport Support, ImmutableArray<ArtifactRenderPack> Packs)
{
    internal ArtifactRenderReportContext Normalize()
    {
        ValidatePairs();
        if (Packs.IsDefault || Packs.Any(pack => pack is null)) throw new InvalidArtifactRenderContract("Effective package pins must be initialized.");
        foreach (var pack in Packs) ValidatePack(pack);
        if (Packs.Select(pack => pack.Name).Distinct(StringComparer.Ordinal).Count() != Packs.Length) throw new InvalidArtifactRenderContract("Effective package pins contain duplicate names.");
        return this with
        {
            Support = Support with { AuditedPairs = [.. Support.AuditedPairs.OrderBy(pair => pair.Language.ToString(), StringComparer.Ordinal).ThenBy(pair => pair.Semantics.ToString(), StringComparer.Ordinal)] },
            Packs = [.. Packs.OrderBy(pack => pack.Name, StringComparer.Ordinal)]
        };
    }

    void ValidatePairs()
    {
        if (Support is null) throw new InvalidArtifactRenderContract("Reports require typed audited provenance.");
        if (Support.AuditedPairs.IsDefaultOrEmpty) throw new InvalidArtifactRenderContract("Reports require exact audited input provenance.");
        ValidatePair(Input);
        foreach (var pair in Support.AuditedPairs) ValidatePair(pair);
        ValidateInputAdmission();
        if (Support.AuditedPairs.Distinct().Count() != Support.AuditedPairs.Length) throw new InvalidArtifactRenderContract("Audited provenance contains duplicate pairs.");
    }

    void ValidateInputAdmission()
    {
        if (Support.DeclaredRange is null)
        {
            if (!Support.AuditedPairs.Contains(Input)) throw new InvalidArtifactRenderContract("The report input is not an exact audited pair.");
            return;
        }

        ValidatePair(Support.DeclaredRange.Minimum);
        if (!Support.DeclaredRange.IsValid) throw new InvalidArtifactRenderContract("The declared range requires a matching minimum pair and a later exclusive major.");
        if (!Support.AuditedPairs.Contains(Input) && !Support.DeclaredRange.Contains(Input)) throw new InvalidArtifactRenderContract("The report input is neither an exact audited pair nor within the declared range.");
    }

    static void ValidatePair(ArtifactRenderVersionPair pair)
    {
        if (pair is null) throw new InvalidArtifactRenderContract("Audited version pairs must be initialized.");
        if (EqualityComparer<LanguageVersion>.Default.Equals(pair.Language, default) || EqualityComparer<SemanticVersion>.Default.Equals(pair.Semantics, default))
            throw new InvalidArtifactRenderContract("Audited version pairs require set language and semantic versions.");
    }

    static void ValidatePack(ArtifactRenderPack pack)
    {
        if (string.IsNullOrWhiteSpace(pack.Name) || string.IsNullOrWhiteSpace(pack.Version)) throw new InvalidArtifactRenderContract("Effective package pins require a name and version.");
        if (pack.Name.Any(char.IsControl) || pack.Version.Any(char.IsControl)) throw new InvalidArtifactRenderContract("Effective package pins cannot contain control characters.");
    }
}
