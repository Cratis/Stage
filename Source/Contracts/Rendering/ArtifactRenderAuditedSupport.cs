// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Provider-supplied audited provenance. Without a declared range this cannot declare a forward range or grant admission;
/// the additive v2 range provenance only reports the range the provider itself applied.
/// </summary>
/// <param name="AuditedPairs">Exact pairs in the provider's audited support ledger.</param>
public sealed record ArtifactRenderAuditedSupport(ImmutableArray<ArtifactRenderVersionPair> AuditedPairs)
{
    /// <summary>The support metadata policy. There is deliberately no string-valued unknown/forward fallback.</summary>
    public const string Policy = "AuditedOnly";

    /// <summary>The audited descriptor schema version.</summary>
    public const string Version = "stage-render-audited-support/v1";

    /// <summary>The v2 policy: exact audited pairs plus a typed newer-within-range declaration.</summary>
    public const string RangePolicy = "AuditedOrNewerWithinDeclaredRange";

    /// <summary>The v2 descriptor schema version, written only when a declared range is supplied.</summary>
    public const string RangeVersion = "stage-render-audited-support/v2";

    /// <summary>Gets the typed range the provider applied, or null for the exact v1 provenance.</summary>
    public ArtifactRenderDeclaredRange? DeclaredRange { get; init; }
}
