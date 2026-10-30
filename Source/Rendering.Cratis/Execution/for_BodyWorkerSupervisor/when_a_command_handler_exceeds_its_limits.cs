// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor;

/// <summary>Stage#891 S5: a throwing, endless or allocating handler is a worker fault, never a refusal, and the same supervisor serves the next call.</summary>
public class when_a_command_handler_exceeds_its_limits : Specification
{
    const string Body = "if (context.Command.Name == \"boom\") throw new System.InvalidOperationException(\"boom\");" +
        " if (context.Command.Name == \"spin\") while (true) { }" +
        " if (context.Command.Name == \"hog\") { var values = new System.Collections.Generic.List<byte[]>(); while (values.Count >= 0) values.Add(new byte[1024 * 1024]); }" +
        " return new OrderPlaced(context.Command.Name);";

    [Theory]
    [InlineData("boom", "faulted", BodyDiagnostics.WorkerFailed)]
    [InlineData("spin", "exceeded-time", BodyDiagnostics.ExceededTime)]
    [InlineData("hog", "exceeded-memory", BodyDiagnostics.ExceededMemory)]
    async Task should_report_the_fault_and_serve_the_next_call_from_the_same_supervisor(string name, string outcome, string code)
    {
        using var fixture = new given.a_real_worker();
        var (loaded, entry) = when_executing_command_handlers.Compile(fixture, Body);
        Assert.True(entry.Admitted, entry.DiagnosticMessage);
        await using var worker = when_executing_command_handlers.Create(fixture, loaded, entry);
        var result = await worker.HandleAsync(when_executing_command_handlers.Request(loaded, entry, name));
        result.Outcome.ShouldEqual(outcome);
        Assert.StartsWith(code, result.Diagnostic, StringComparison.Ordinal);
        var next = await worker.HandleAsync(when_executing_command_handlers.Request(loaded, entry, "ada"));
        Assert.True(next.Outcome == "produced", next.Diagnostic);
    }
}
#endif
