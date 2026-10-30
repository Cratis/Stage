// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution.for_ScreenplayBodyExecutionProvider.given;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ScreenplayBodyExecutionProvider;

public class when_disposed_from_a_blocking_context : Specification
{
    [Fact]
    async Task should_kill_and_reap_an_active_worker_without_capturing_the_blocked_context()
    {
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        const string script = "printf '%s\\n' '{\"kind\":\"hello\",\"protocolVersion\":1,\"workerVersion\":\"fixture\"}'\nread -r line\nread -r another";
        var worker = new BodyWorkerSupervisor(new(string.Empty, Path.Combine(root, ".ai-work", "891", "specs", Guid.NewGuid().ToString("N")))
        {
            Launch = new("/bin/sh", ["-c", script]),
            TimeoutMs = 5000
        });
        await using var provider = new ScreenplayBodyExecutionProvider(new("revision", []), worker);
        var pending = worker.SendAsync(new("active", "evaluate"));
        var pid = worker.ProcessId;
        pid.ShouldNotBeNull();
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => DisposeSynchronously(provider, disposed)) { IsBackground = true };
        thread.Start();
        await disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        (await pending).Evaluation!.Outcome.ShouldEqual("faulted");
        worker.ProcessId.ShouldBeNull();
        Assert.Throws<ArgumentException>(() => Process.GetProcessById(pid!.Value));
        await provider.DisposeAsync();
    }

    static void DisposeSynchronously(ScreenplayBodyExecutionProvider provider, TaskCompletionSource completed)
    {
        SynchronizationContext.SetSynchronizationContext(new a_blocked_context());
        try
        {
            provider.DisposeAsync().AsTask().GetAwaiter().GetResult();
            completed.TrySetResult();
        }
        catch (Exception exception)
        {
            completed.TrySetException(exception);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }
}
