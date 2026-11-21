// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerProcess;

public class when_the_process_exits_during_memory_sampling : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_return_the_response_when_process_statistics_become_unavailable(bool nativeFailure)
    {
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        var cache = Path.Combine(root, ".ai-work", "stage-1164", "bodyworker-specs", Guid.NewGuid().ToString("N"));
        const string script = """
            printf '%s\n' '{"kind":"hello","protocolVersion":2,"workerVersion":"fixture"}'
            read -r request
            printf '%s\n' '{"id":"probe","evaluation":{"outcome":"satisfied","messages":[]}}'
            read -r next
            """;
        var options = new BodyWorkerOptions(string.Empty, cache) { Launch = new("/bin/sh", ["-c", script]) };
        var samples = 0;
        await using var worker = new BodyWorkerProcess(options, "execute", process =>
        {
            samples++;

            // EOF lets the fixture exit only after it has written the complete response.
            process.StandardInput.Close();
            Assert.True(process.WaitForExit(5000));
            throw nativeFailure ? new Win32Exception(3) : new InvalidOperationException("Process has exited.");
        });
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await worker.StartAsync(deadline.Token);

        var response = await worker.ExchangeAsync(new("probe", "evaluate"), deadline.Token);

        response.Id.ShouldEqual("probe");
        response.Evaluation!.Outcome.ShouldEqual("satisfied");
        samples.ShouldEqual(1);
        worker.ExceededMemory.ShouldBeFalse();
    }
}
