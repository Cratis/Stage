// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Host-supplied executable and resource limits, independent of any rendering target.</summary>
public sealed record BodyWorkerOptions(string WorkerPath, string CacheDirectory)
{
    /// <summary>Gets an optional executable/argument launch for the CLI's embedded worker.</summary>
    public BodyWorkerLaunch? Launch { get; init; }

    /// <summary>Gets the .NET executable used for a framework-dependent worker.</summary>
    public string DotnetPath { get; init; } = "dotnet";

    /// <summary>Gets the startup and per-evaluation wall-time limit in milliseconds.</summary>
    public int TimeoutMs { get; init; } = 2000;

    /// <summary>Gets the separate startup and compilation wall-time limit in milliseconds.</summary>
    public int CompileTimeoutMs { get; init; } = 120000;

    /// <summary>Gets the execute-role managed heap limit in MiB, not an OS memory cap.</summary>
    public int HeapLimitMb { get; init; } = 256;

    /// <summary>Gets the execute-role working-set watchdog cap in MiB.</summary>
    public int WorkingSetLimitMb { get; init; } = 512;

    /// <summary>Gets the maximum bytes in a response line.</summary>
    public int MaxResponseBytes { get; init; } = 1024 * 1024;

    /// <summary>Gets the bounded stderr capacity in bytes.</summary>
    public int MaxStderrBytes { get; init; } = 64 * 1024;

    /// <summary>Read the optional --body-worker-* process arguments.</summary>
    /// <param name="args">The runner arguments.</param>
    /// <returns>Worker options, or null when no worker arguments were supplied.</returns>
    public static BodyWorkerOptions? FromArguments(string[] args)
    {
        const string prefix = "--body-worker-";
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index++)
        {
            if (!args[index].StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (index + 1 == args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"Missing value for '{args[index]}'.");
            values[args[index][prefix.Length..]] = args[++index];
        }
        if (values.Count == 0) return null;
        return new(WorkerPathArgument(values), values.GetValueOrDefault("cache", Path.Combine(Path.GetTempPath(), "cratis-stage-body-cache")))
        {
            DotnetPath = values.GetValueOrDefault("dotnet", "dotnet"),
            TimeoutMs = LimitArgument(values, "timeout-ms", 2000),
            CompileTimeoutMs = LimitArgument(values, "compile-timeout-ms", 120000),
            HeapLimitMb = LimitArgument(values, "heap-limit-mb", 256),
            WorkingSetLimitMb = LimitArgument(values, "working-set-limit-mb", 512),
            MaxResponseBytes = LimitArgument(values, "max-response-bytes", 1024 * 1024),
            MaxStderrBytes = LimitArgument(values, "max-stderr-bytes", 64 * 1024)
        };
    }

    static string WorkerPathArgument(Dictionary<string, string> values) =>
        values.TryGetValue("path", out var path) && !string.IsNullOrWhiteSpace(path) ? path : throw new ArgumentException("Body worker path is required.");

    static int LimitArgument(Dictionary<string, string> values, string key, int fallback)
    {
        if (!values.TryGetValue(key, out var value)) return fallback;
        if (int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number) && number > 0) return number;
        throw new ArgumentException($"Body worker '{key}' must be a positive integer.");
    }
}
