// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Limits how many generated-application build and test processes the specs run at once.
/// </summary>
/// <remarks>
/// Each generated application starts its own dotnet or npm process tree. Unbounded, a local spec run starts
/// many of them in parallel and starves the machine. STAGE_MAX_PARALLEL_APP_BUILDS sets the limit; without
/// it the limit is a quarter of the processors locally and unlimited in CI.
/// </remarks>
public static class GeneratedApplicationBuilds
{
    /// <summary>
    /// The environment variable that sets the limit.
    /// </summary>
    public const string LimitVariable = "STAGE_MAX_PARALLEL_APP_BUILDS";

    static readonly SemaphoreSlim? _slots = Limit(
        Environment.GetEnvironmentVariable(LimitVariable),
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase),
        Environment.ProcessorCount) is { } limit ? new SemaphoreSlim(limit, limit) : null;

    /// <summary>
    /// Waits for a free slot before a build or test process starts.
    /// </summary>
    /// <returns>The slot, released when disposed.</returns>
    public static async Task<IDisposable> Enter()
    {
        if (_slots is null)
        {
            return new Slot(null);
        }

        await _slots.WaitAsync();
        return new Slot(_slots);
    }

    /// <summary>
    /// Resolves the limit from its configured value, whether the run is in CI, and the processor count.
    /// </summary>
    /// <param name="configured">The value of <see cref="LimitVariable"/>, if any.</param>
    /// <param name="ci">Whether the run is in CI.</param>
    /// <param name="processors">The processor count.</param>
    /// <returns>The number of concurrent processes, or null for no limit.</returns>
    /// <exception cref="ArgumentException">The configured value is not a positive whole number.</exception>
    public static int? Limit(string? configured, bool ci, int processors)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return int.TryParse(configured, out var value) && value > 0
                ? value
                : throw new ArgumentException($"{LimitVariable} must be a positive whole number, not '{configured}'.", nameof(configured));
        }

        return ci ? null : Math.Max(1, processors / 4);
    }

    sealed class Slot(SemaphoreSlim? slots) : IDisposable
    {
        int _released;

        public void Dispose()
        {
            if (slots is not null && Interlocked.Exchange(ref _released, 1) == 0)
            {
                slots.Release();
            }
        }
    }
}
