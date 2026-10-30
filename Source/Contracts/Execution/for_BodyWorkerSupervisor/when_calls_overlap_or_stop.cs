// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerSupervisor;

public class when_calls_overlap_or_stop : Specification
{
    const string Hello = "printf '%s\\n' '{\"kind\":\"hello\",\"protocolVersion\":2,\"workerVersion\":\"fixture\"}'";

    [Fact]
    async Task should_serialize_concurrent_requests_on_one_worker()
    {
        var script = $$$"""
            {{{Hello}}}
            while IFS= read -r line; do
              case "$line" in
                *first*) id=first ;;
                *) id=second ;;
              esac
              printf '{"id":"%s","evaluation":{"outcome":"satisfied","messages":[]}}\n' "$id"
            done
            """;
        await using var supervisor = Supervisor(script);
        var first = supervisor.SendAsync(new("first", "evaluate"));
        var pid = supervisor.ProcessId;
        var second = supervisor.SendAsync(new("second", "evaluate"));
        var responses = await Task.WhenAll(first, second);
        responses.Select(response => response.Id).ShouldEqual(["first", "second"]);
        responses.All(response => response.Evaluation.Outcome == "satisfied").ShouldBeTrue();
        supervisor.ProcessId.ShouldEqual(pid);
    }

    [Fact]
    async Task should_propagate_caller_cancellation_and_reap_the_worker()
    {
        await using var supervisor = Supervisor($"{Hello}\nread -r line\nread -r another");
        using var cancellation = new CancellationTokenSource();
        var pending = supervisor.SendAsync(new("cancel", "evaluate"), cancellation.Token);
        supervisor.ProcessId.ShouldNotBeNull();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        supervisor.ProcessId.ShouldBeNull();
    }

    [Fact]
    async Task should_cancel_active_and_queued_calls_when_disposed_and_never_restart()
    {
        var supervisor = Supervisor($"{Hello}\nread -r line\nread -r another");
        var active = supervisor.SendAsync(new("active", "evaluate"));
        var queued = supervisor.SendAsync(new("queued", "evaluate"));
        var disposing = supervisor.DisposeAsync().AsTask();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
        (await active).Evaluation.Outcome.ShouldEqual("faulted");
        await disposing;
        await supervisor.DisposeAsync();
        supervisor.ProcessId.ShouldBeNull();
        (await supervisor.SendAsync(new("after", "evaluate"))).Evaluation.Outcome.ShouldEqual("faulted");
    }

    static BodyWorkerSupervisor Supervisor(string script)
    {
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        return new(new(string.Empty, Path.Combine(root, ".ai-work", "891", "specs", Guid.NewGuid().ToString("N")))
        {
            Launch = new("/bin/sh", ["-c", script]),
            TimeoutMs = 5000
        });
    }
}
