// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_cancelling_a_real_process_tree : Specification
{
    // A hang guard only: the specification proceeds on signals (the child's PID arriving, the descendant exiting), never on elapsed time.
    static readonly TimeSpan _hangGuard = TimeSpan.FromMinutes(2);

    // The descendant outlives the hang guard by far, so only the kill can end it before the guard expires.
    const string DescendantSeconds = "3600";

    readonly CancellationTokenSource _cancellation = new();
    readonly CancellationTokenSource _guard = new(_hangGuard);
    string _directory = null!;
    string _signal = null!;
    Task<SpecificationProcessResult> _run = null!;
    Process _child = null!;
    Exception? _error;

    async Task Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-process-spec-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _signal = Path.Combine(_directory, "started");

        // A named pipe: the child's write blocks until the spec reads, so the PID arrives as a signal rather than a file to watch for.
        using var fifo = Process.Start("mkfifo", [_signal]);
        await fifo.WaitForExitAsync(_guard.Token);
    }

    async Task Because()
    {
        var start = new ProcessStartInfo("/bin/sh") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add($"sleep {DescendantSeconds} & child=$!; printf '%s' \"$child\" > \"$1\"; wait \"$child\"");
        start.ArgumentList.Add("stage-spec");
        start.ArgumentList.Add(_signal);
        _run = new SpecificationRunProcess().Run(start, _cancellation.Token);
        var pid = await Task.Run(() => File.ReadAllText(_signal), _guard.Token).WaitAsync(_guard.Token);
        _child = Process.GetProcessById(int.Parse(pid, System.Globalization.CultureInfo.InvariantCulture));
        await _cancellation.CancelAsync();
        _error = await Catch.Exception(() => _run);

        // Kill signals the tree, but waiting for the parent alone does not observe a descendant's exit.
        await _child.WaitForExitAsync(_guard.Token);
    }

    [Fact] void should_interrupt_the_run() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_kill_the_descendant() => _child.HasExited.ShouldBeTrue();

    async Task Destroy()
    {
        await _cancellation.CancelAsync();
        if (_run is not null) await ((Task)_run).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (_child is { HasExited: false }) _child.Kill(entireProcessTree: true);
        _child?.Dispose();
        _cancellation.Dispose();
        _guard.Dispose();
        Directory.Delete(_directory, recursive: true);
    }
}
