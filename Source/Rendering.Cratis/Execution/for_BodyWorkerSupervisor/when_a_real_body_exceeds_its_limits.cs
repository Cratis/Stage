// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor;

public class when_a_real_body_exceeds_its_limits : Specification
{
    [Theory]
    [InlineData("System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite); return true;", BodyDiagnostics.ExceededTime)]
    [InlineData("while (true) { }", BodyDiagnostics.ExceededTime)]
    [InlineData("var values = new System.Collections.Generic.List<byte[]>(); while (true) values.Add(new byte[1024 * 1024]);", BodyDiagnostics.ExceededMemory)]
    async Task should_kill_it_report_a_stable_code_and_respawn_only_for_the_next_call(string body, string code)
    {
        var fixture = new a_real_worker();
        var good = fixture.StressUnit("return context.Value > 0;");
        var bad = fixture.StressUnit(body);
        await using var supervisor = fixture.Create();
        var healthy = await supervisor.SendAsync(fixture.Request(good));
        Assert.True(healthy.Evaluation!.Outcome == "satisfied", healthy.Evaluation.Diagnostic);
        var pid = supervisor.ProcessId!.Value;
        var result = await supervisor.SendAsync(fixture.Request(bad), CancellationToken.None);
        result.Evaluation!.Outcome.ShouldEqual(code == BodyDiagnostics.ExceededTime ? "exceeded-time" : "exceeded-memory");
        Assert.StartsWith(code, result.Evaluation.Diagnostic, StringComparison.Ordinal);
        supervisor.ProcessId.ShouldBeNull();
        var error = Catch.Exception(() =>
        {
            using var process = Process.GetProcessById(pid);
            process.HasExited.ShouldBeTrue();
        });
        error.ShouldBeOfExactType<ArgumentException>();
        (await supervisor.SendAsync(fixture.Request(good))).Evaluation!.Outcome.ShouldEqual("satisfied");
        supervisor.ProcessId.ShouldNotEqual(pid);
    }
}
