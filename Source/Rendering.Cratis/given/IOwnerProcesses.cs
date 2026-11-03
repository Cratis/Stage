// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Finds out whether the process that owned a container still runs on this host.
/// </summary>
public interface IOwnerProcesses
{
    /// <summary>
    /// Gets the state of a process identified by id and start time.
    /// </summary>
    /// <param name="processId">The recorded process id.</param>
    /// <param name="startedUnixSeconds">The recorded process start time in Unix seconds.</param>
    /// <returns>The state of that exact process.</returns>
    OwnerProcessState State(int processId, long startedUnixSeconds);
}
