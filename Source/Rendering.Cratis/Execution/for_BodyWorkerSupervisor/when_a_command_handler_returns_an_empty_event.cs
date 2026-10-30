// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor;

/// <summary>Stage#891 S5: a declared event without properties is a valid handler result and serializes as an empty composite.</summary>
public class when_a_command_handler_returns_an_empty_event
{
    [Fact]
    async Task should_return_an_empty_composite()
    {
        const string source = "module Orders\n  feature Ordering\n    slice StateChange Place\n      command PlaceOrder\n        orderId Uuid identifier\n        name String\n        handler\n          implementation\n            hint \"Normalize the name\"\n        produces event Noted\n          for orderId\n";
        using var fixture = new given.a_real_worker();
        var (loaded, entry) = when_executing_command_handlers.Compile(fixture, "return new Noted();", source);
        Assert.True(entry.Admitted, entry.DiagnosticMessage);
        await using var worker = when_executing_command_handlers.Create(fixture, loaded, entry);
        var result = await worker.HandleAsync(when_executing_command_handlers.Request(loaded, entry, "ada"));
        Assert.True(result.Outcome == "produced", result.Diagnostic);
        var events = Assert.IsType<SemanticArrayValue>(SemanticValueWire.Read(result.Value!.Value));
        Assert.Empty(Assert.IsType<SemanticCompositeValue>(Assert.Single(events.Values)).Properties);
    }
}
#endif
