// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Probes real operating system processes.
/// </summary>
public sealed class OwnerProcesses : IOwnerProcesses
{
    const long StartTolerance = 1;

    /// <summary>
    /// Gets the start time of a process in Unix seconds.
    /// </summary>
    /// <param name="process">The process.</param>
    /// <returns>The truncated start time.</returns>
    public static long StartedUnixSeconds(Process process) => new DateTimeOffset(process.StartTime.ToUniversalTime()).ToUnixTimeSeconds();

    /// <inheritdoc/>
    public OwnerProcessState State(int processId, long startedUnixSeconds)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                return OwnerProcessState.Dead;
            }

            return Math.Abs(StartedUnixSeconds(process) - startedUnixSeconds) <= StartTolerance
                ? OwnerProcessState.Alive
                : OwnerProcessState.Replaced;
        }
        catch (ArgumentException)
        {
            return OwnerProcessState.Dead;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            return OwnerProcessState.Unknown;
        }
    }
}
