// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerSupervisor;

public class when_the_worker_is_unavailable : Specification
{
    [Fact]
    async Task should_refuse_without_loading_a_body_into_the_host()
    {
        await using var supervisor = new BodyWorkerSupervisor(new("missing-worker.dll", Path.Combine(AppContext.BaseDirectory, "worker-cache"))
        {
            DotnetPath = "missing-dotnet"
        });
        var response = await supervisor.SendAsync(new("one", "evaluate"));
        response.Evaluation!.Outcome.ShouldEqual("unsupported");
        response.Evaluation.Diagnostic!.StartsWith(BodyDiagnostics.WorkerFailed, StringComparison.Ordinal).ShouldBeTrue();
        supervisor.ProcessId.ShouldBeNull();
    }
}
