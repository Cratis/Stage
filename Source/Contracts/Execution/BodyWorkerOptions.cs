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
}
