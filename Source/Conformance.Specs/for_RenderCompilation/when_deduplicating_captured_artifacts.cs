// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_RenderCompilation.given;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderCompilation;

public class when_deduplicating_captured_artifacts : Specification
{
    artifact_compilations<int> _cache = null!;
    ImmutableArray<PlannedArtifact> _artifacts;
    readonly TaskCompletionSource<int> _compiler = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<int>[] _duplicates = [];
    IReadOnlyDictionary<string, Task<int>> _mapped = null!;
    int _invocations;
    int[] _failures = [];

    void Establish()
    {
        _cache = new();
        _artifacts = [PlannedArtifact.CreateText("App.cs", "class App;"), PlannedArtifact.CreateBinary("data.bin", [0, 1, 255])];
    }

    async Task Because()
    {
        var concurrent = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Task.Run(() =>
        {
            // Return the task as a value so all concurrent callers arrive before the compiler is released.
            var task = _cache.Get([.. _artifacts.Reverse()], Compile);
            return new[] { task };
        })));
        _duplicates = [.. concurrent.SelectMany(tasks => tasks)];
        _mapped = new Dictionary<string, Task<int>>(StringComparer.Ordinal)
        {
            ["AcmeAdmissionsCorpus.V29"] = _cache.Get(_artifacts, Compile),
            ["EventRoutesCorpus.CompositesV8"] = _cache.Get([.. _artifacts.Reverse()], Compile)
        };
        _compiler.SetResult(1); // A nonzero compiler outcome is data, never promoted to success.
        _failures = await Task.WhenAll(_mapped.Values);
        foreach (var changed in new ImmutableArray<PlannedArtifact>[]
        {
            [PlannedArtifact.CreateText("Renamed.cs", "class App;"), _artifacts[1]],
            [PlannedArtifact.CreateText("App.cs", "class Changed;"), _artifacts[1]],
            [_artifacts[0], PlannedArtifact.CreateBinary("data.bin", [0, 2, 255])],
            [_artifacts[0]],
            [.. _artifacts, PlannedArtifact.CreateText("Extra.cs", "class Extra;")]
        })
        {
            await _cache.Get(changed, Compile);
        }
        await _cache.Get(_artifacts, Compile);
    }

    Task<int> Compile()
    {
        Interlocked.Increment(ref _invocations);
        return _compiler.Task;
    }

    [Fact] void should_share_one_task_for_reordered_concurrent_duplicates() => Assert.All(_duplicates, task => Assert.Same(_mapped.Values.First(), task));
    [Fact] void should_compile_each_changed_path_byte_or_artifact_set()
    {
        Assert.Equal(6, _invocations);
        Assert.Equal(6, _cache.Invocations);
        Assert.Equal(20, _cache.Requests);
    }
    [Fact] void should_keep_failed_results_for_every_requesting_fixture()
    {
        Assert.Equal<string>(["AcmeAdmissionsCorpus.V29", "EventRoutesCorpus.CompositesV8"], _mapped.Keys.Order(StringComparer.Ordinal));
        Assert.Equal<int>([1, 1], _failures);
        Assert.All(_mapped.Values, task => Assert.Same(_duplicates[0], task));
    }
}
#endif
