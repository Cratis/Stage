// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Limits how many generated-application build and test processes the specs run at once.
/// </summary>
/// <remarks>
/// Each generated application starts its own dotnet or npm process tree. Unbounded, a spec run starts
/// many of them in parallel and starves the machine. By default one runs at a time, locally and in CI;
/// STAGE_MAX_PARALLEL_APP_BUILDS sets a higher limit explicitly. Each dotnet build or test is also
/// capped at <see cref="MaxCpuCount"/> MSBuild nodes.
/// </remarks>
public static class GeneratedApplicationBuilds
{
    /// <summary>
    /// The environment variable that sets the limit.
    /// </summary>
    public const string LimitVariable = "STAGE_MAX_PARALLEL_APP_BUILDS";

    /// <summary>
    /// The most MSBuild nodes one generated-application dotnet build or test may use.
    /// </summary>
    public const int MaxCpuCount = 3;

    static readonly SemaphoreSlim _slots = new(Limit(Environment.GetEnvironmentVariable(LimitVariable)));

    /// <summary>
    /// Waits for a free slot before a build or test process starts.
    /// </summary>
    /// <returns>The slot, released when disposed.</returns>
    public static async Task<IDisposable> Enter()
    {
        await _slots.WaitAsync();
        return new Slot(_slots);
    }

    /// <summary>
    /// Resolves the limit from its configured value.
    /// </summary>
    /// <param name="configured">The value of <see cref="LimitVariable"/>, if any.</param>
    /// <returns>The number of concurrent processes; one when nothing is configured.</returns>
    /// <exception cref="ArgumentException">The configured value is not a positive whole number.</exception>
    public static int Limit(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return 1;
        }

        return int.TryParse(configured, out var value) && value > 0
            ? value
            : throw new ArgumentException($"{LimitVariable} must be a positive whole number, not '{configured}'.", nameof(configured));
    }

    /// <summary>
    /// Adds the MSBuild node cap to a dotnet build or test that does not set one.
    /// </summary>
    /// <param name="fileName">The tool being started.</param>
    /// <param name="arguments">The tool's arguments.</param>
    /// <returns>The arguments, with the cap right after the dotnet verb when one was missing.</returns>
    public static string[] Capped(string fileName, string[] arguments) =>
        string.Equals(fileName, "dotnet", StringComparison.Ordinal)
        && arguments.Length > 0
        && arguments[0] is "build" or "test"
        && !arguments.Any(argument => argument.StartsWith("-m:", StringComparison.Ordinal))
            ? [arguments[0], $"-m:{MaxCpuCount}", .. arguments.Skip(1)]
            : arguments;

    sealed class Slot(SemaphoreSlim slots) : IDisposable
    {
        int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                slots.Release();
            }
        }
    }
}
