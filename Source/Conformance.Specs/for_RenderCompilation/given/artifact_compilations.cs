// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Conformance.Specs.for_RenderCompilation.given;

// Cache the complete path+byte set, not model identity, plan digest, source files alone or a successful exit.
public sealed class artifact_compilations<TResult>
{
    readonly ConcurrentDictionary<string, Lazy<Task<TResult>>> _compilations = new(StringComparer.Ordinal);
    int _invocations;
    int _requests;

    public int Invocations => _invocations;
    public int Requests => _requests;

    public Task<TResult> Get(ImmutableArray<PlannedArtifact> artifacts, Func<Task<TResult>> compile)
    {
        Interlocked.Increment(ref _requests);
        return _compilations.GetOrAdd(Key(artifacts), static (_, state) => new(() => state.Cache.Invoke(state.Compile), LazyThreadSafetyMode.ExecutionAndPublication), (Cache: this, Compile: compile)).Value;
    }

    async Task<TResult> Invoke(Func<Task<TResult>> compile)
    {
        Interlocked.Increment(ref _invocations);
        return await compile();
    }

    static string Key(ImmutableArray<PlannedArtifact> artifacts)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(artifacts.Length);
        foreach (var artifact in artifacts.OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal))
        {
            writer.Write(artifact.RelativePath.Length);
            foreach (var character in artifact.RelativePath)
            {
                writer.Write((ushort)character);
            }
            writer.Write(artifact.Bytes.Length);
            writer.Write(artifact.Bytes.AsSpan());
        }
        writer.Flush();

        // Lossless encoding avoids relying on a hash collision assumption for cache identity.
        return Convert.ToBase64String(stream.ToArray());
    }
}
#endif
