// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Identifies whether a gap needs a payload implementation or an unsupported rendering capability.
/// </summary>
public enum ArtifactImplementationGapKind
{
    /// <summary>
    /// A generated payload extension point needs an implementation.
    /// </summary>
    Implementation = 0,

    /// <summary>
    /// A semantic owner cannot be rendered; it may be omitted rather than represented by a payload stub.
    /// </summary>
    Rendering = 1
}
