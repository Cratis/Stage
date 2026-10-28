// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Contracts.Rendering;

public sealed partial class ArtifactRenderPlan
{
    /// <summary>Gets the explicit selected-leaf inventory, when supplied. Legacy factories do not infer one.</summary>
    public ArtifactRenderInventory? Inventory { get; private init; }

    /// <summary>Gets the input model's exact typed versions, retained for validating report provenance.</summary>
    public ArtifactRenderVersionPair? InputVersions { get; private init; }

    /// <summary>
    /// Gets the effective input provenance: profile input digests, implementation inputs and planner components. Prerelease.
    /// It is excluded from <see cref="Digest"/> and has its own <see cref="ArtifactRenderInputProvenance.Digest"/>.
    /// </summary>
    public ArtifactRenderInputProvenance InputProvenance { get; private init; } = ArtifactRenderInputProvenance.Empty;

    /// <summary>Records the planner components that produced this plan, replacing any recorded earlier.</summary>
    /// <param name="components">The components. Duplicate kind and name pairs, blank names or versions, and malformed digests are invalid.</param>
    /// <returns>A new immutable plan retaining artifacts, scopes, acceptance and the output-only digest.</returns>
    /// <exception cref="InvalidArtifactRenderContract">A component is malformed or duplicated.</exception>
    public ArtifactRenderPlan WithInputComponents(ImmutableArray<ArtifactRenderComponent> components)
    {
        if (components.IsDefault || components.Any(IsMalformed))
        {
            throw new InvalidArtifactRenderContract("Input components require a defined kind, a name, a version and an optional lowercase SHA-256 digest.");
        }

        if (components.GroupBy(_ => (_.Kind, _.Name)).Any(_ => _.Count() > 1))
        {
            throw new InvalidArtifactRenderContract("Input components contain a duplicate kind and name.");
        }

        var sorted = components.OrderBy(_ => _.Kind).ThenBy(_ => _.Name, StringComparer.Ordinal).ToImmutableArray();
        return ApplyAcceptance(Degradations, Strict, Inventory, provenance: InputProvenance with { Components = sorted });
    }

    /// <summary>Attaches a validated caller-supplied inventory and verifies its evidence against original diagnostics.</summary>
    /// <param name="inventory">The semantic inventory selected by the provider, not discovered from artifact paths.</param>
    /// <returns>A new immutable plan retaining artifacts, scopes, acceptance and validated removals.</returns>
    public ArtifactRenderPlan WithInventory(ArtifactRenderInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        if (Degradations.Any(gap => !inventory.Evidence.Contains(gap))) throw new InvalidArtifactRenderContract("An inventory cannot hide existing degradation evidence.");
        ValidateRefusalEvidence(inventory);
        return ApplyAcceptance(inventory.Evidence, Strict, inventory);
    }

    static bool IsMalformed(ArtifactRenderComponent? component) =>
        component is null || !Enum.IsDefined(component.Kind) || component.Kind == ArtifactRenderComponentKind.Unknown ||
        string.IsNullOrWhiteSpace(component.Name) || string.IsNullOrWhiteSpace(component.Version) ||
        (component.Digest is not null && (component.Digest.Length != 64 || !component.Digest.All(char.IsAsciiHexDigitLower)));

    void ValidateRefusalEvidence(ArtifactRenderInventory inventory)
    {
        foreach (var construct in inventory.Constructs)
        {
            if (construct.Outcome == ArtifactRenderOutcome.Refused &&
                construct.RefusalReasons.All(reason => reason.Severity == ArtifactRenderDiagnosticSeverity.Warning) && !HasRecordedOmission(construct))
            {
                throw new InvalidArtifactRenderContract("Inventory omission evidence does not match an actual recorded omission.");
            }
            foreach (var reason in construct.RefusalReasons) ValidateOriginalReason(reason);
        }
    }

    bool HasRecordedOmission(ArtifactRenderConstruct construct) => construct.Kind == ArtifactRenderConstructKind.Specification && Refusals.Any(refusal =>
        refusal.Artifact == construct.Identity && refusal.Kind == "specification" &&
        refusal.Codes.ToHashSet(StringComparer.Ordinal).SetEquals(construct.RefusalReasons.Select(reason => reason.Code)));

    void ValidateOriginalReason(ArtifactRenderDiagnostic reason)
    {
        if (GenerationDiagnostics.Contains(reason)) return;
        throw new InvalidArtifactRenderContract(reason.Severity == ArtifactRenderDiagnosticSeverity.Error ?
            "Inventory refusal evidence does not match an original generation error." : "Inventory omission evidence does not match an original generation warning.");
    }

    string InventoryDigest(string original)
    {
        if (Inventory is null) return original;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.Write(ArtifactRenderInventory.Version);
        writer.Write(original);
        writer.Write(Inventory.Digest);
        var scopes = ArtifactRenderReports.Scopes(this).ToArray();
        writer.Write(scopes.Length);
        foreach (var scope in scopes)
        {
            writer.Write(scope.Kind.ToString());
            writer.Write(scope.Artifact.ToString());
        }
        writer.Flush();
        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }
}
