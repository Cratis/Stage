// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Provider-supplied audited provenance only. This data type cannot declare a forward range or grant admission.</summary>
/// <param name="AuditedPairs">Exact pairs in the provider's audited support ledger.</param>
public sealed record ArtifactRenderAuditedSupport(ImmutableArray<ArtifactRenderVersionPair> AuditedPairs)
{
    /// <summary>The support metadata policy. There is deliberately no string-valued unknown/forward fallback.</summary>
    public const string Policy = "AuditedOnly";

    /// <summary>The audited descriptor schema version.</summary>
    public const string Version = "stage-render-audited-support/v1";
}
