// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Kits;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>Caller compatibility constraints; these cannot make an unsupported contract compatible.</summary>
public sealed record ContractOverrides(ImmutableArray<ContractVersion> Supported)
{
    /// <summary>Gets no additional constraints.</summary>
    public static ContractOverrides None { get; } = new([]);
}
