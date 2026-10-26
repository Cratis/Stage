// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>A validated caller-supplied selection of semantic leaves. This is not model discovery or version admission.</summary>
public sealed class ArtifactRenderInventory
{
    /// <summary>The canonical inventory shape and ordering version.</summary>
    public const string Version = "stage-render-inventory/v1";

    ArtifactRenderInventory(ImmutableArray<ArtifactRenderConstruct> constructs)
    {
        Constructs = constructs;
        Score = ArtifactRenderScore.For(constructs);
    }

    /// <summary>Gets owners ordered by ordinal model path, kind and identity.</summary>
    public ImmutableArray<ArtifactRenderConstruct> Constructs { get; }

    /// <summary>Gets the total selected-owner score.</summary>
    public ArtifactRenderScore Score { get; }

    /// <summary>Gets the aggregate realization outcome, independent of acceptance.</summary>
    public ArtifactRenderOutcome Outcome => ArtifactRenderOutcomeClassification.AggregateInventory(Constructs);

    /// <summary>Gets a versioned metadata digest, including graph, placeholders and fixed scoring policy.</summary>
    public string Digest => Convert.ToHexString(SHA256.HashData(ArtifactRenderInventoryWriter.Bytes(this))).ToLowerInvariant();

    internal ImmutableArray<ArtifactRenderDegradation> Evidence => [.. Constructs.SelectMany(construct => construct.Degradations.Select(gap => gap.Evidence))];

    /// <summary>Validates selected owners, original gap shapes and the entire root/cascade DAG before returning an inventory.</summary>
    /// <param name="constructs">Explicit selected semantic leaves, never artifact counts.</param>
    /// <returns>The normalized immutable inventory.</returns>
    public static ArtifactRenderInventory Create(ImmutableArray<ArtifactRenderConstruct> constructs) => new(ArtifactRenderInventoryValidation.Normalize(constructs));
}
