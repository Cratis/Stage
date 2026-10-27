// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Counts semantic owners, not artifacts or diagnostic records.</summary>
/// <param name="Rendered">Rendered owners.</param>
/// <param name="Degraded">Degraded owners, including cascades.</param>
/// <param name="Refused">Refused owners.</param>
public sealed record ArtifactRenderCounts(int Rendered, int Degraded, int Refused)
{
    /// <summary>Gets the selected owner count.</summary>
    public int Total => Rendered + Degraded + Refused;
}
