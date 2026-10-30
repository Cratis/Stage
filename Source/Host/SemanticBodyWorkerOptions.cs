// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Execution;
using Microsoft.Extensions.Configuration;

namespace Cratis.Stage.Host;

internal sealed record SemanticBodyWorkerOptions(bool Enabled, string WorkerPath)
{
    internal string CacheDirectory { get; init; } = BodyWorkerCacheDirectory.Default;
    internal int CallTimeoutMilliseconds { get; init; } = 2000;
    internal int MemoryLimitMegabytes { get; init; } = 256;
    internal int CompileTimeoutSeconds { get; init; } = 120;
    internal string? CatalogPath { get; init; }

    internal static SemanticBodyWorkerOptions From(IConfiguration configuration) => new(
        configuration.GetValue("Stage:BodyWorker:Enabled", true),
        configuration["Stage:BodyWorker:WorkerPath"] ?? Path.Combine(AppContext.BaseDirectory, "bodyworker", "Cratis.Stage.BodyWorker.dll"))
    {
        CacheDirectory = configuration["Stage:BodyWorker:CacheDirectory"] ?? BodyWorkerCacheDirectory.Default,
        CallTimeoutMilliseconds = Math.Clamp(configuration.GetValue("Stage:BodyWorker:CallTimeoutMilliseconds", 2000), 50, 30000),
        MemoryLimitMegabytes = Math.Clamp(configuration.GetValue("Stage:BodyWorker:MemoryLimitMegabytes", 256), 32, 4096),
        CompileTimeoutSeconds = Math.Clamp(configuration.GetValue("Stage:BodyWorker:CompileTimeoutSeconds", 120), 5, 600)
    };

    internal BodyWorkerOptions Worker() => new(Path.GetFullPath(WorkerPath, AppContext.BaseDirectory), Path.GetFullPath(CacheDirectory))
    {
        TimeoutMs = CallTimeoutMilliseconds,
        HeapLimitMb = MemoryLimitMegabytes,
        WorkingSetLimitMb = MemoryLimitMegabytes * 2,
        CompileTimeoutMs = CompileTimeoutSeconds * 1000
    };
}
