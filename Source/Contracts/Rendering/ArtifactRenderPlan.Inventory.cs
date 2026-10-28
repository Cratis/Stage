// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Contracts.Rendering;

public sealed partial class ArtifactRenderPlan
{
    /// <summary>Gets the explicit selected-leaf inventory, when supplied. Legacy factories do not infer one.</summary>
    public ArtifactRenderInventory? Inventory { get; private init; }

    /// <summary>Gets the input model's exact typed versions, retained for validating report provenance.</summary>
    public ArtifactRenderVersionPair? InputVersions { get; private init; }

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
