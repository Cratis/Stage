// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// What is known about the process that owned a container.
/// </summary>
public enum OwnerProcessState
{
    /// <summary>The process is running and started at the recorded time.</summary>
    Alive,

    /// <summary>No such process exists.</summary>
    Dead,

    /// <summary>A process with the recorded id exists but started at a different time, so the owner is gone.</summary>
    Replaced,

    /// <summary>The state could not be determined.</summary>
    Unknown
}
