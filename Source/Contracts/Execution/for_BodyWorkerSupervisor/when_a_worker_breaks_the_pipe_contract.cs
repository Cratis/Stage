// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerSupervisor;

public class when_a_worker_breaks_the_pipe_contract : Specification
{
    const string Hello = "{\"kind\":\"hello\",\"protocolVersion\":1,\"workerVersion\":\"fixture\"}";
    const string Response = "{\"id\":\"probe\",\"evaluation\":{\"outcome\":\"satisfied\",\"messages\":[]}}";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_bound_startup_or_blocked_stdin_kill_and_allow_the_next_call(bool helloFirst)
    {
        var cache = Cache();
        var script = $$"""
            if [ ! -e started ]; then
              : > started
              {{(helloFirst ? $"printf '%s\\n' '{Hello}'" : string.Empty)}}
              sleep 60 &
              echo $! > child.pid
              wait
              exit 0
            fi
            {{Healthy()}}
            """;
        await using var supervisor = Supervisor(cache, script, timeout: 500);
        var request = new BodyWorkerRequest("probe", "evaluate")
        {
            Evaluation = new("fixture", ImmutableDictionary<string, JsonElement>.Empty.Add("Value", JsonSerializer.SerializeToElement(new string('x', 2 * 1024 * 1024))))
        };
        var watch = Stopwatch.StartNew();
        var pending = supervisor.SendAsync(request);
        var pid = supervisor.ProcessId!.Value;
        var result = await pending;
        Assert.StartsWith(BodyDiagnostics.ExceededTime, result.Evaluation!.Diagnostic, StringComparison.Ordinal);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5));
        supervisor.ProcessId.ShouldBeNull();
        Gone(pid).ShouldBeTrue();
        var child = int.Parse(await File.ReadAllTextAsync(Path.Combine(cache, "child.pid")), System.Globalization.CultureInfo.InvariantCulture);
        Gone(child).ShouldBeTrue();
        (await supervisor.SendAsync(new("probe", "evaluate"))).Evaluation!.Outcome.ShouldEqual("satisfied");
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"id\":\"wrong\",\"evaluation\":{\"outcome\":\"satisfied\",\"messages\":[]}}")]
    [InlineData("{\"id\":\"probe\",\"evaluation\":{\"outcome\":\"unknown\",\"messages\":[]}}")]
    async Task should_kill_a_bad_response_without_retrying_the_same_call(string response)
    {
        var cache = Cache();
        var script = $$"""
            printf '%s\n' '{{Hello}}'
            if [ ! -e started ]; then
              : > started
              read -r line
              printf '%s\n' '{{response}}'
              sleep 60
              exit 0
            fi
            while IFS= read -r line; do printf '%s\n' '{{Response}}'; done
            """;
        await using var supervisor = Supervisor(cache, script, timeout: 30000);
        var result = await supervisor.SendAsync(new("probe", "evaluate"));
        Assert.StartsWith(BodyDiagnostics.WorkerFailed, result.Evaluation!.Diagnostic, StringComparison.Ordinal);
        supervisor.ProcessId.ShouldBeNull();
        (await supervisor.SendAsync(new("probe", "evaluate"))).Evaluation!.Outcome.ShouldEqual("satisfied");
    }

    static string Healthy() => $"printf '%s\\n' '{Hello}'\nwhile IFS= read -r line; do printf '%s\\n' '{Response}'; done";

    static BodyWorkerSupervisor Supervisor(string cache, string script, int timeout = 2000) => new(new(string.Empty, cache)
    {
        Launch = new("/bin/sh", ["-c", script]),
        TimeoutMs = timeout
    });

    static string Cache()
    {
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        var path = Path.Combine(root, ".ai-work", "891", "specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    static bool Gone(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.HasExited;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }
}
