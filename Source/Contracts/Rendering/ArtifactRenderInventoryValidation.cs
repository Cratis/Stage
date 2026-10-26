// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

static class ArtifactRenderInventoryValidation
{
    internal static ImmutableArray<ArtifactRenderConstruct> Normalize(ImmutableArray<ArtifactRenderConstruct> constructs)
    {
        ValidateOwners(constructs);
        foreach (var construct in constructs) Validate(construct);
        var normalized = constructs.Select(NormalizeConstruct).OrderBy(construct => construct.SemanticLocator, ArtifactRenderSemanticLocatorComparer.Instance)
            .ThenBy(construct => construct.Identity.ToString(), StringComparer.Ordinal).ToImmutableArray();
        ArtifactRenderInventoryGraph.Validate(normalized);
        return normalized;
    }

    internal static void ValidatePath(string? path)
    {
        if (path is null) return;
        if (!IsPortable(path)) throw new InvalidArtifactRenderContract("A model path must be relative, nonempty and portable.");
        if (path.Split('/').Any(IsTraversal)) throw new InvalidArtifactRenderContract("A model path cannot be absolute or traverse directories.");
    }

    static void ValidateOwners(ImmutableArray<ArtifactRenderConstruct> constructs)
    {
        if (constructs.IsDefault) throw new InvalidArtifactRenderContract("An inventory requires initialized semantic owners.");
        if (constructs.Any(construct => construct is null)) throw new InvalidArtifactRenderContract("An inventory cannot contain null owners.");
        if (constructs.Select(construct => construct.Identity).Distinct().Count() != constructs.Length) throw new InvalidArtifactRenderContract("An inventory contains duplicate semantic owners.");
    }

    static ArtifactRenderConstruct NormalizeConstruct(ArtifactRenderConstruct construct) => construct with
    {
        Degradations = [.. construct.Degradations.OrderBy(gap => gap.Evidence.Diagnostic.Code, StringComparer.Ordinal).Select(gap => gap with
        {
            Roots = [.. gap.Roots.OrderBy(root => root.Owner.ToString(), StringComparer.Ordinal).ThenBy(root => root.Code, StringComparer.Ordinal)]
        })],
        RefusalReasons = [.. construct.RefusalReasons.OrderBy(reason => reason.Code, StringComparer.Ordinal).ThenBy(reason => reason.Message, StringComparer.Ordinal)]
    };

    static void Validate(ArtifactRenderConstruct construct)
    {
        if (!HasKnownIdentity(construct)) throw new InvalidArtifactRenderContract("A construct requires a set identity, known kind and outcome.");
        if (construct.SemanticLocator is null) throw new InvalidArtifactRenderContract("A construct requires an exact typed semantic locator.");
        ValidatePath(construct.ModelPath);
        ValidateState(construct);
        foreach (var gap in construct.Degradations) ValidateGap(construct, gap);
        if (construct.Degradations.Select(gap => gap.Evidence.Diagnostic.Code).Distinct(StringComparer.Ordinal).Count() != construct.Degradations.Length) throw new InvalidArtifactRenderContract("An owner contains duplicate degradation codes.");
        foreach (var reason in construct.RefusalReasons) ValidateReason(construct, reason);
    }

    static void ValidateState(ArtifactRenderConstruct construct)
    {
        if (construct.Degradations.IsDefault || construct.RefusalReasons.IsDefault) throw new InvalidArtifactRenderContract("Construct detail collections must be initialized.");
        if (!StateMatches(construct)) throw new InvalidArtifactRenderContract("Construct details do not match the declared outcome.");
    }

    static bool StateMatches(ArtifactRenderConstruct construct) => construct.Outcome switch
    {
        ArtifactRenderOutcome.Rendered => construct.Degradations.IsEmpty && construct.RefusalReasons.IsEmpty,
        ArtifactRenderOutcome.Degraded => !construct.Degradations.IsEmpty && construct.RefusalReasons.IsEmpty,
        _ => construct.Degradations.IsEmpty && !construct.RefusalReasons.IsEmpty
    };

    static void ValidateGap(ArtifactRenderConstruct construct, ArtifactRenderConstructGap gap)
    {
        if (gap?.Evidence is null) throw new InvalidArtifactRenderContract("A gap requires original warning evidence.");
        ArtifactRenderDegradation.Normalize([gap.Evidence], [gap.Evidence.Diagnostic]);
        if (gap.Evidence.Diagnostic.Artifact != construct.Identity) throw new InvalidArtifactRenderContract("Gap evidence belongs to another owner.");
        if (!Enum.IsDefined(gap.PlaceholderKind)) throw new InvalidArtifactRenderContract("A gap requires a known placeholder.");
        if (string.IsNullOrWhiteSpace(gap.Intent)) throw new InvalidArtifactRenderContract("A gap requires modeled intent.");
        if (gap.ModelPath != construct.ModelPath) throw new InvalidArtifactRenderContract("Gap and owner model paths differ.");
        if (!construct.SemanticLocator.Equals(gap.SemanticLocator)) throw new InvalidArtifactRenderContract("Gap and owner semantic locators differ.");
        ValidateReferences(gap);
    }

    static void ValidateReferences(ArtifactRenderConstructGap gap)
    {
        if (gap.Roots.IsDefault) throw new InvalidArtifactRenderContract("Gap references must be initialized.");
        if (gap.Roots.Any(IsInvalidReference)) throw new InvalidArtifactRenderContract("Gap references require an owner and code.");
        if (gap.Roots.Distinct().Count() != gap.Roots.Length) throw new InvalidArtifactRenderContract("Gap references contain duplicates.");
        var dependent = gap.Evidence.Diagnostic.Code == ArtifactRenderDegradationCodes.DependencyDegradation;
        if (dependent == gap.Roots.IsEmpty) throw new InvalidArtifactRenderContract("Only 005 dependency gaps carry nonempty cause references.");
    }

    static void ValidateReason(ArtifactRenderConstruct construct, ArtifactRenderDiagnostic reason)
    {
        if (!IsOwnerError(construct, reason)) throw new InvalidArtifactRenderContract("A refusal requires an original generation error on its owner.");
        if (string.IsNullOrWhiteSpace(reason.Code) || string.IsNullOrWhiteSpace(reason.Message)) throw new InvalidArtifactRenderContract("Refusal reasons require a code and explanation.");
    }

    static bool HasKnownIdentity(ArtifactRenderConstruct construct) => construct.Identity.IsSet && Enum.IsDefined(construct.Kind) && Enum.IsDefined(construct.Outcome);
    static bool IsOwnerError(ArtifactRenderConstruct construct, ArtifactRenderDiagnostic reason) => reason is not null && reason.Artifact == construct.Identity && reason.Severity == ArtifactRenderDiagnosticSeverity.Error;
    static bool IsInvalidReference(ArtifactRenderGapReference root) => root?.Owner.IsSet != true || string.IsNullOrWhiteSpace(root.Code);
    static bool IsPortable(string path) => !string.IsNullOrWhiteSpace(path) && !path.Any(char.IsControl) && !path.Contains(':') && !path.Contains('\\');
    static bool IsTraversal(string part) => part.Length == 0 || part == "." || part == "..";
}
