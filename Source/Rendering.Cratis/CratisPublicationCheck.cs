// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Describes whether a Cratis plan can replace destination artifacts without changing stored event identities or losing unselected policies.
/// </summary>
public abstract record CratisPublicationCheck
{
    private CratisPublicationCheck()
    {
    }

#pragma warning disable CA1034 // These nested records are the closed, named outcomes of the publication check.
    /// <summary>
    /// The plan preserves stored event identities and does not overwrite an incompatible aggregate policy layout.
    /// </summary>
    public sealed record Compatible : CratisPublicationCheck;

    /// <summary>
    /// The destination must first be migrated by rendering the entire application, rather than publishing this scoped plan.
    /// </summary>
    /// <param name="Paths">The incompatible application-root-relative paths in ordinal order.</param>
    /// <param name="Reason">The reason publication requires application scope.</param>
    public sealed record RequiresApplicationScope(ImmutableArray<string> Paths, string Reason) : CratisPublicationCheck;
    /// <summary>
    /// Publication would change a stored event identity, or the existing identity cannot be determined safely.
    /// </summary>
    /// <param name="Changes">The event identity changes in ordinal path and type order.</param>
    /// <param name="Diagnostics">The errors explaining why publication is refused.</param>
    public sealed record ChangesEventTypeIds(ImmutableArray<CratisEventTypeIdChange> Changes, ImmutableArray<ArtifactRenderDiagnostic> Diagnostics) : CratisPublicationCheck;
#pragma warning restore CA1034
}
