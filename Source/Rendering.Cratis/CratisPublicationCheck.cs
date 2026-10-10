// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Describes whether a Cratis plan can replace the destination's shared policy layout without losing unselected policies.
/// </summary>
public abstract record CratisPublicationCheck
{
    private CratisPublicationCheck()
    {
    }

#pragma warning disable CA1034 // These nested records are the closed, named outcomes of the publication check.
    /// <summary>
    /// The plan does not overwrite an incompatible aggregate policy layout.
    /// </summary>
    public sealed record Compatible : CratisPublicationCheck;

    /// <summary>
    /// The destination must first be migrated by rendering the entire application, rather than publishing this scoped plan.
    /// </summary>
    /// <param name="Paths">The incompatible application-root-relative paths in ordinal order.</param>
    /// <param name="Reason">The reason publication requires application scope.</param>
    public sealed record RequiresApplicationScope(ImmutableArray<string> Paths, string Reason) : CratisPublicationCheck;
#pragma warning restore CA1034
}
