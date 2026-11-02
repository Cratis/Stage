// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Limits how many generated-application build and test processes the specs run at once.
/// </summary>
/// <remarks>
/// Each generated application starts its own dotnet or npm process tree. Unbounded, a spec run starts
/// many of them in parallel and starves the machine. By default one runs at a time, locally and in CI;
/// STAGE_MAX_PARALLEL_APP_BUILDS sets a higher limit explicitly. Each dotnet build or test is also
/// capped at <see cref="MaxCpuCount"/> MSBuild nodes. STAGE_GENERATED_APP_TIMINGS optionally records
/// slot wait and hold seconds; STAGE_GENERATED_APP_BINLOG optionally enables build and restore binlogs.
/// </remarks>
public static class GeneratedApplicationBuilds
{
    /// <summary>
    /// The environment variable that sets the limit.
    /// </summary>
    public const string LimitVariable = "STAGE_MAX_PARALLEL_APP_BUILDS";

    /// <summary>
    /// The environment variable naming the per-project timing TSV to append.
    /// </summary>
    public const string TimingsVariable = "STAGE_GENERATED_APP_TIMINGS";

    /// <summary>
    /// The environment variable enabling generated build and restore binlogs.
    /// </summary>
    public const string BinlogVariable = "STAGE_GENERATED_APP_BINLOG";

    /// <summary>
    /// The most MSBuild nodes one generated-application dotnet build or test may use.
    /// </summary>
    public const int MaxCpuCount = 3;

    static readonly SemaphoreSlim _slots = new(Limit(Environment.GetEnvironmentVariable(LimitVariable)));
    static readonly Lock _timingsLock = new();

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
    /// Waits for a free slot and optionally records the process's wait and hold times.
    /// </summary>
    /// <param name="process">The process about to start.</param>
    /// <returns>The slot, recorded and released when disposed.</returns>
    public static async Task<IDisposable> Enter(GeneratedApplicationProcess process)
    {
        var started = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        await _slots.WaitAsync();
        return new TimedSlot(Environment.GetEnvironmentVariable(TimingsVariable), started, process, stopwatch, stopwatch.Elapsed);
    }

    /// <summary>
    /// Adds an optional binlog to a build or restore without changing an existing binlog.
    /// </summary>
    /// <param name="arguments">The dotnet arguments.</param>
    /// <param name="enabled">The value of <see cref="BinlogVariable"/>.</param>
    /// <param name="directory">The evidence directory.</param>
    /// <param name="logName">The process log's name.</param>
    /// <returns>The original arguments, or the arguments with a binlog appended.</returns>
    public static string[] WithBinlog(string[] arguments, string? enabled, string directory, string logName) =>
        (enabled == "1" || string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
        && arguments.Length > 0 && (string.Equals(arguments[0], "build", StringComparison.Ordinal) || string.Equals(arguments[0], "restore", StringComparison.Ordinal))
        && !arguments.Any(argument => argument.Equals("-bl", StringComparison.OrdinalIgnoreCase)
            || argument.Equals("/bl", StringComparison.OrdinalIgnoreCase)
            || argument.StartsWith("-bl:", StringComparison.OrdinalIgnoreCase)
            || argument.StartsWith("/bl:", StringComparison.OrdinalIgnoreCase))
            ? [.. arguments, $"-bl:{Path.Combine(directory, Path.GetFileNameWithoutExtension(logName))}.binlog"]
            : arguments;

    /// <summary>
    /// Formats one process's timing as invariant tab-separated values.
    /// </summary>
    /// <param name="started">When the slot was requested.</param>
    /// <param name="process">The process that held the slot.</param>
    /// <param name="waited">Time waiting for the slot.</param>
    /// <param name="held">Time holding the slot.</param>
    /// <returns>The timestamp, caller, tool and verb, configuration, wait and hold seconds.</returns>
    public static string TimingLine(DateTimeOffset started, GeneratedApplicationProcess process, TimeSpan waited, TimeSpan held) =>
        string.Join(
            '\t',
            started.ToString("O", CultureInfo.InvariantCulture),
            process.Caller,
            process.Verb,
            process.Configuration,
            waited.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture),
            held.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture));

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
        && (string.Equals(arguments[0], "build", StringComparison.Ordinal) || string.Equals(arguments[0], "test", StringComparison.Ordinal))
        && !arguments.Any(argument => argument.StartsWith("-m:", StringComparison.Ordinal))
            ? [arguments[0], $"-m:{MaxCpuCount}", .. arguments.Skip(1)]
            : arguments;

    sealed class TimedSlot(string? path, DateTimeOffset started, GeneratedApplicationProcess process, Stopwatch stopwatch, TimeSpan waited) : IDisposable
    {
        int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0) return;
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                var line = TimingLine(started, process, waited, stopwatch.Elapsed - waited);
                lock (_timingsLock)
                {
                    var header = File.Exists(path) ? string.Empty : "started\tcaller\tprocess\tconfiguration\twait_seconds\thold_seconds" + Environment.NewLine;
                    File.AppendAllText(path, header + line + Environment.NewLine);
                }
            }
            finally
            {
                _slots.Release();
            }
        }
    }

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
