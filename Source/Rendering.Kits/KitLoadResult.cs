// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Kits;

/// <summary>Either a complete kit or sorted rejection diagnostics, never a partial kit.</summary>
public sealed record KitLoadResult(LoadedKit? Kit, ImmutableArray<KitDiagnostic> Diagnostics)
{
    /// <summary>Gets whether a complete kit was loaded.</summary>
    public bool Succeeded => Kit is not null && Diagnostics.IsEmpty;
}
