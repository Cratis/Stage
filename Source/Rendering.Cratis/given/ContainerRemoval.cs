// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// The outcome of a guarded container removal.
/// </summary>
public enum ContainerRemoval
{
    /// <summary>No container was created, so nothing was done.</summary>
    NotCreated,

    /// <summary>The container was removed by its full identifier.</summary>
    Removed,

    /// <summary>Docker confirmed the container no longer exists.</summary>
    AlreadyGone,

    /// <summary>The container exists but is not owned by this run, so it was left alone.</summary>
    Refused,

    /// <summary>Ownership could not be established, so nothing was removed.</summary>
    Unknown,

    /// <summary>Ownership was proven but the removal command failed.</summary>
    Failed
}
