// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor;

/// <summary>Stage#891 S5: a handler returning a tuple yields one payload per produced event, in produces order.</summary>
public class when_a_command_handler_produces_several_events : Specification
{
    [Theory]
    [InlineData(3)]
    [InlineData(8)]
    async Task should_return_the_payloads_in_produces_order(int count)
    {
        var names = Enumerable.Range(1, count).Select(index => $"Echoed{index}").ToArray();
        var source = "module Orders\n  feature Ordering\n    slice StateChange Place\n      command PlaceOrder\n        orderId Uuid identifier\n        name String\n        handler\n          implementation\n            hint \"Normalize the name\"\n" +
            string.Concat(names.Select(name => $"        produces event {name}\n          for orderId\n          name String = name\n"));
        var body = "return (" + string.Join(", ", names.Select((name, index) => $"new {name}(context.Command.Name + \"{index + 1}\")")) + ");";
        using var fixture = new a_real_worker();
        var (loaded, entry) = when_executing_command_handlers.Compile(fixture, body, source);
        Assert.True(entry.Admitted, entry.DiagnosticMessage);
        await using var worker = when_executing_command_handlers.Create(fixture, loaded, entry);
        var result = await worker.HandleAsync(when_executing_command_handlers.Request(loaded, entry, "ada"));
        Assert.True(result.Outcome == "produced", result.Diagnostic);
        var events = Assert.IsType<SemanticArrayValue>(SemanticValueWire.Read(result.Value!.Value));
        events.Values.Select(value => Assert.IsType<SemanticCompositeValue>(value).Properties.Single().Value)
            .ShouldEqual([.. Enumerable.Range(1, count).Select(index => SemanticValue.Text("ada" + index))]);
    }
}
#endif
