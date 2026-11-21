// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerProcess;

public class when_a_memory_sample_fails_while_the_process_is_running : Specification
{
    [Fact]
    async Task should_keep_enforcing_the_working_set_limit()
    {
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        var cache = Path.Combine(root, ".ai-work", "stage-1164", "bodyworker-specs", Guid.NewGuid().ToString("N"));
        const string script = """
            printf '%s\n' '{"kind":"hello","protocolVersion":2,"workerVersion":"fixture"}'
            read -r request
            sleep 30
            """;
        var options = new BodyWorkerOptions(string.Empty, cache) { Launch = new("/bin/sh", ["-c", script]) };
        var samples = 0;
        await using var worker = new BodyWorkerProcess(options, "execute", _ =>
        {
            samples++;
            if (samples == 1) throw new Win32Exception(5);
            return long.MaxValue;
        });
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await worker.StartAsync(deadline.Token);

        Exception? failure = null;
        try
        {
            await worker.ExchangeAsync(new("probe", "evaluate"), deadline.Token);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        failure.ShouldNotBeNull();
        samples.ShouldBeGreaterThan(1);
        worker.ExceededMemory.ShouldBeTrue();
    }
}
