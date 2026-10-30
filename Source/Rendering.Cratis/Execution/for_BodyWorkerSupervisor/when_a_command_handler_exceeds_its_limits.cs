// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor;

/// <summary>Stage#891 S5: a throwing, endless or allocating handler is a worker fault, never a refusal, and the next call still works.</summary>
public class when_a_command_handler_exceeds_its_limits : Specification
{
    [Theory]
    [InlineData("if (context.Command.Name.Length >= 0) throw new System.InvalidOperationException(\"boom\"); return new OrderPlaced(context.Command.Name);", "faulted", BodyDiagnostics.WorkerFailed)]
    [InlineData("if (context.Command.Name.Length >= 0) while (true) { } return new OrderPlaced(context.Command.Name);", "exceeded-time", BodyDiagnostics.ExceededTime)]
    [InlineData("var values = new System.Collections.Generic.List<byte[]>(); while (values.Count >= 0) values.Add(new byte[1024 * 1024]); return new OrderPlaced(context.Command.Name);", "exceeded-memory", BodyDiagnostics.ExceededMemory)]
    async Task should_report_the_fault_and_serve_the_next_call(string body, string outcome, string code)
    {
        using var fixture = new a_real_worker();
        var (loaded, entry) = when_executing_command_handlers.Compile(fixture, body);
        Assert.True(entry.Admitted, entry.DiagnosticMessage);
        await using var worker = when_executing_command_handlers.Create(fixture, loaded, entry);
        var result = await worker.HandleAsync(when_executing_command_handlers.Request(loaded, entry, "ada"));
        result.Outcome.ShouldEqual(outcome);
        Assert.StartsWith(code, result.Diagnostic, StringComparison.Ordinal);
        var (goodLoaded, good) = when_executing_command_handlers.Compile(fixture, handler_model.Body);
        await using var next = when_executing_command_handlers.Create(fixture, goodLoaded, good);
        (await next.HandleAsync(when_executing_command_handlers.Request(goodLoaded, good, "ada"))).Outcome.ShouldEqual("produced");
    }
}
#endif
