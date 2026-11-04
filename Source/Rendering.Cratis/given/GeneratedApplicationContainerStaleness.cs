// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Decides whether a labelled container left by an earlier run may be reaped.
/// </summary>
/// <remarks>
/// <see cref="MaxRuntime"/> is far above the worst-case life of a healthy run: an 8 minute create and pull, a 2 minute
/// readiness wait, a 2 minute host wait, and the port, log and removal commands add up to well under an hour. A container
/// older than that is a leak. Liveness is only judged for owners on this host; another host can only be judged by age.
/// </remarks>
public static class GeneratedApplicationContainerStaleness
{
    /// <summary>
    /// The age above which a labelled container is a leak whoever owns it.
    /// </summary>
    public static readonly TimeSpan MaxRuntime = TimeSpan.FromHours(2);

    /// <summary>
    /// Decides to reap or keep.
    /// </summary>
    /// <param name="owner">The owner read from the container labels, or null when missing or malformed.</param>
    /// <param name="current">The owner of the current run.</param>
    /// <param name="now">The current time.</param>
    /// <param name="processes">Probes the owning process.</param>
    /// <returns>The decision.</returns>
    public static StalenessDecision Decide(GeneratedApplicationContainerOwner? owner, GeneratedApplicationContainerOwner current, DateTimeOffset now, IOwnerProcesses processes)
    {
        if (owner is null)
        {
            return new(false, "kept: owner labels are missing or malformed");
        }

        if (owner.RunToken == current.RunToken)
        {
            return new(false, "kept: it belongs to the current run");
        }

        var age = now - DateTimeOffset.FromUnixTimeSeconds(owner.StartedUnixSeconds);
        if (age < TimeSpan.Zero)
        {
            return new(false, "kept: its start time is in the future");
        }

        if (age > MaxRuntime)
        {
            return new(true, $"reaped: age {age.TotalMinutes:F0} min exceeds {MaxRuntime.TotalMinutes:F0} min");
        }

        return owner.Host == current.Host ? DecideByProcess(owner, processes, age) : new(false, $"kept: another host, age {age.TotalMinutes:F0} min");
    }

    static StalenessDecision DecideByProcess(GeneratedApplicationContainerOwner owner, IOwnerProcesses processes, TimeSpan age) =>
        processes.State(owner.ProcessId, owner.ProcessStartedUnixSeconds) switch
        {
            OwnerProcessState.Dead => new(true, $"reaped: owner process is gone, age {age.TotalMinutes:F0} min"),
            OwnerProcessState.Replaced => new(true, $"reaped: owner process id was reused, age {age.TotalMinutes:F0} min"),
            OwnerProcessState.Alive => new(false, $"kept: owner process is alive, age {age.TotalMinutes:F0} min"),
            _ => new(false, $"kept: owner process state is unknown, age {age.TotalMinutes:F0} min")
        };
}
