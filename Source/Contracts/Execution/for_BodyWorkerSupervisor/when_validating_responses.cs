// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerSupervisor;

public class when_validating_responses : Specification
{
    [Fact]
    async Task should_reject_a_version_one_worker_before_sending_a_request()
    {
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        var cache = Path.Combine(root, ".ai-work", "891", "specs", Guid.NewGuid().ToString("N"));
        var hello = JsonSerializer.Serialize(new BodyWorkerHello("hello", 1, "old-worker"), BodyWorkerMessages.Json);
        var script = $"printf '%s\\n' '{hello}'\nread -r line";
        await using var worker = new BodyWorkerSupervisor(new(string.Empty, cache) { Launch = new("/bin/sh", ["-c", script]), TimeoutMs = 10000 });
        var result = await worker.SendAsync(new("probe", "validate"));
        Assert.StartsWith(BodyDiagnostics.WorkerFailed, result.Evaluation!.Diagnostic, StringComparison.Ordinal);
        worker.ProcessId.ShouldBeNull();
    }

    [Theory]
    [InlineData("validate", "satisfied", "empty", true)]
    [InlineData("validate", "unsatisfied", "messages", true)]
    [InlineData("validate", "unsatisfied", "empty", false)]
    [InlineData("validate", "state", "state", false)]
    [InlineData("validate", "delete", "empty", false)]
    [InlineData("reduce", "state", "state", true)]
    [InlineData("reduce", "state", "empty", false)]
    [InlineData("reduce", "delete", "empty", true)]
    [InlineData("reduce", "satisfied", "empty", false)]
    [InlineData("evaluate", "state", "state", false)]
    [InlineData("validate", "refused", "empty", true)]
    [InlineData("reduce", "faulted", "empty", true)]
    async Task should_accept_only_the_outcomes_of_the_requested_kind(string kind, string outcome, string payload, bool valid)
    {
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        var cache = Path.Combine(root, ".ai-work", "891", "specs", Guid.NewGuid().ToString("N"));
        var response = new BodyWorkerResponse("probe") { Evaluation = new(outcome, payload == "state" ? JsonSerializer.SerializeToElement(1) : null, payload == "messages" ? ["first"] : []) };
        var hello = JsonSerializer.Serialize(new BodyWorkerHello("hello", BodyWorkerMessages.ProtocolVersion, "fixture"), BodyWorkerMessages.Json);
        var script = $"printf '%s\\n' '{hello}'\nread -r line\nprintf '%s\\n' '{JsonSerializer.Serialize(response, BodyWorkerMessages.Json)}'\nread -r another";
        await using var worker = new BodyWorkerSupervisor(new(string.Empty, cache) { Launch = new("/bin/sh", ["-c", script]), TimeoutMs = 10000 });
        var result = await worker.SendAsync(new("probe", kind));
        if (valid)
        {
            result.Evaluation!.Outcome.ShouldEqual(outcome);
        }
        else
        {
            Assert.StartsWith(BodyDiagnostics.WorkerFailed, result.Evaluation!.Diagnostic, StringComparison.Ordinal);
            worker.ProcessId.ShouldBeNull();
        }
    }
}
