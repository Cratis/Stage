// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Stage.Contracts.Rendering;

public sealed partial class ArtifactRenderPlan
{
    ImmutableArray<ArtifactRenderDiagnostic>? _generationDiagnostics;

    /// <summary>Gets the validated root degradations, in ordinal semantic-identity, code and message order.</summary>
    public ImmutableArray<ArtifactRenderDegradation> Degradations { get; private init; } = [];

    /// <summary>
    /// Gets plan-level degradation evidence, in ordinal order. It is validated against original warnings and promoted by strict acceptance
    /// like a degradation, but it names no selected construct, so it is excluded from the inventory, completeness score and construct gaps.
    /// </summary>
    public ImmutableArray<ArtifactRenderDegradation> PlanEvidence { get; private init; } = [];

    /// <summary>Gets whether validated degradations fail acceptance. Defaults to false.</summary>
    public bool Strict { get; private init; }

    /// <summary>Gets whether original generation diagnostics permit publication, independently of strict acceptance.</summary>
    public bool CanPublishArtifacts => GenerationDiagnostics.All(diagnostic => diagnostic.Severity != ArtifactRenderDiagnosticSeverity.Error);

    bool HasAcceptance => Strict || !Degradations.IsEmpty || !PlanEvidence.IsEmpty;

    ImmutableArray<ArtifactRenderDiagnostic> GenerationDiagnostics => _generationDiagnostics ?? Diagnostics;

    /// <summary>
    /// Applies acceptance to an already validated plan, retaining artifacts, gaps, scopes, refusals and removals.
    /// Only exact original warnings recorded as degradations are promoted; unrelated errors remain unsafe.
    /// This does not authorize publication or change legacy factories. Applying acceptance again uses original diagnostics.
    /// </summary>
    /// <param name="degradations">Original warning evidence and remedies. Default arrays and duplicate warnings are invalid.</param>
    /// <param name="strict">Whether recorded warnings become acceptance errors.</param>
    /// <returns>A new immutable plan. The original plan is unchanged.</returns>
    /// <exception cref="InvalidArtifactRenderContract">Evidence is malformed or does not match an original public degradation warning.</exception>
    public ArtifactRenderPlan WithAcceptance(ImmutableArray<ArtifactRenderDegradation> degradations, bool strict = false) => ApplyAcceptance(degradations, strict, Inventory);

    /// <summary>Records plan-level, non-scored degradation evidence. Applying acceptance or an inventory afterwards retains it.</summary>
    /// <param name="evidence">Original warning evidence and remedies. Default arrays and duplicate warnings are invalid.</param>
    /// <returns>A new immutable plan. A non-empty set makes a publishable plan at least degraded.</returns>
    /// <exception cref="InvalidArtifactRenderContract">Evidence is malformed or does not match an original public degradation warning.</exception>
    public ArtifactRenderPlan WithPlanEvidence(ImmutableArray<ArtifactRenderDegradation> evidence) =>
        ApplyAcceptance(Degradations, Strict, Inventory, ArtifactRenderDegradation.Normalize(evidence, GenerationDiagnostics));

    ArtifactRenderPlan ApplyAcceptance(ImmutableArray<ArtifactRenderDegradation> degradations, bool strict, ArtifactRenderInventory? inventory, ImmutableArray<ArtifactRenderDegradation>? planEvidence = null)
    {
        var retained = planEvidence ?? PlanEvidence;
        var original = GenerationDiagnostics;
        var normalized = ArtifactRenderDegradation.Normalize(degradations, original);
        if (inventory is not null && !normalized.ToHashSet().SetEquals(inventory.Evidence)) throw new InvalidArtifactRenderContract("Acceptance evidence must agree with the attached inventory.");
        var evidence = normalized.Concat(retained).Select(gap => gap.Diagnostic).ToHashSet();
        var accepted = original.Select(diagnostic => strict && evidence.Contains(diagnostic)
            ? diagnostic with { Severity = ArtifactRenderDiagnosticSeverity.Error } : diagnostic).ToImmutableArray();
        return new(Target, TargetVersion, Renderer, RendererVersion, ApplicationName, SemanticRevision, Artifacts, accepted, ImplementationGaps, Refusals, RequiredRemovals)
        {
            Scope = Scope,
            AdditionalScopes = AdditionalScopes,
            Degradations = normalized,
            PlanEvidence = retained,
            Strict = strict,
            InputVersions = InputVersions,
            InlineCodeFingerprint = InlineCodeFingerprint,
            _generationDiagnostics = original,
            Inventory = inventory
        };
    }

    string AcceptanceDigest(string original)
    {
        if (!HasAcceptance) return original;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.Write("stage-acceptance-v1");
        writer.Write(original);
        writer.Write(Strict);
        writer.Write(Degradations.Length);
        foreach (var gap in Degradations)
        {
            writer.Write(gap.Diagnostic.Artifact.ToString());
            writer.Write(gap.Diagnostic.Code);
            writer.Write(gap.Diagnostic.Message);
            writer.Write(gap.Remedy);
        }
        if (!PlanEvidence.IsEmpty)
        {
            writer.Write("plan-evidence");
            writer.Write(PlanEvidence.Length);
            foreach (var gap in PlanEvidence)
            {
                writer.Write(gap.Diagnostic.Artifact.ToString());
                writer.Write(gap.Diagnostic.Code);
                writer.Write(gap.Diagnostic.Message);
                writer.Write(gap.Remedy);
            }
        }
        writer.Flush();
        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }
}
