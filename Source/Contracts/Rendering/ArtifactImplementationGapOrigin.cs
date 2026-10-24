// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Identifies why a requirement has no implementation.
/// </summary>
public enum ArtifactImplementationGapOrigin
{
    /// <summary>
    /// Unspecified origins are invalid.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Behavior is described by an explicit pending implementation intent.
    /// </summary>
    Described = 1,

    /// <summary>
    /// A performer body could not be verified or rendered and remains an explicit compilable gap.
    /// </summary>
    Unverified = 2
}
