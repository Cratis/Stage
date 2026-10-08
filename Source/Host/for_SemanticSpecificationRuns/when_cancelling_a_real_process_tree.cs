// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_cancelling_a_real_process_tree : Specification
{
    readonly CancellationTokenSource _cancellation = new(TimeSpan.FromSeconds(30));
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string _directory = null!;
    string _signal = null!;
    FileSystemWatcher _watcher = null!;
    Task<SpecificationProcessResult> _run = null!;
    Process _child = null!;
    Exception? _error;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-process-spec-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _signal = Path.Combine(_directory, "started");
        _watcher = new FileSystemWatcher(_directory);
        _watcher.Created += (_, args) => { if (args.FullPath == _signal) _started.TrySetResult(); };
        _watcher.Renamed += (_, args) => { if (args.FullPath == _signal) _started.TrySetResult(); };
        _watcher.EnableRaisingEvents = true;
    }

    async Task Because()
    {
        var start = new ProcessStartInfo("/bin/sh") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("sleep 60 & child=$!; printf '%s' \"$child\" > \"$1.tmp\"; mv \"$1.tmp\" \"$1\"; wait \"$child\"");
        start.ArgumentList.Add("stage-spec");
        start.ArgumentList.Add(_signal);
        _run = new SpecificationRunProcess().Run(start, _cancellation.Token);
        await _started.Task.WaitAsync(_cancellation.Token);
        _child = Process.GetProcessById(int.Parse(await File.ReadAllTextAsync(_signal), System.Globalization.CultureInfo.InvariantCulture));
        await _cancellation.CancelAsync();
        _error = await Catch.Exception(() => _run);

        // Kill signals the tree, but waiting for the parent alone does not observe a descendant's exit.
        using var terminated = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _child.WaitForExitAsync(terminated.Token);
    }

    [Fact] void should_interrupt_the_run() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_kill_the_descendant() => _child.HasExited.ShouldBeTrue();

    async Task Destroy()
    {
        await _cancellation.CancelAsync();
        if (_run is not null) await ((Task)_run).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        _child?.Dispose();
        _watcher.Dispose();
        _cancellation.Dispose();
        Directory.Delete(_directory, recursive: true);
    }
}
