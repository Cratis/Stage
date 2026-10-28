// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Host-supplied executable and resource limits, independent of any rendering target.</summary>
public sealed record BodyWorkerOptions(string WorkerPath, string CacheDirectory)
{
    /// <summary>Gets the .NET executable used for a framework-dependent worker.</summary>
    public string DotnetPath { get; init; } = Environment.ProcessPath ?? "dotnet";

    /// <summary>Gets the per-evaluation wall-time limit in milliseconds.</summary>
    public int TimeoutMs { get; init; } = 3000;

    /// <summary>Gets the separate startup and compilation wall-time limit in milliseconds.</summary>
    public int CompileTimeoutMs { get; init; } = 30000;

    /// <summary>Gets the managed heap limit in MiB.</summary>
    public int HeapLimitMb { get; init; } = 96;

    /// <summary>Gets the maximum bytes in a response line.</summary>
    public int MaxResponseBytes { get; init; } = 1024 * 1024;

    /// <summary>Gets the bounded stderr capacity.</summary>
    public int MaxStderrBytes { get; init; } = 4096;
}
