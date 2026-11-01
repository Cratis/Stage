// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_an_inline_return_type_that_collides_with_a_framework_contract : state_change_slices
{
    [Theory]
    [InlineData("EventForEventSourceId")]
    [InlineData("global::EventForEventSourceId")]
    public void should_refuse_to_choose_between_the_modeled_event_and_the_framework_contract(string typeName)
    {
        var command = _processBatch.Slice.Commands.Single();
        var body = $"return new {typeName}();";
        command = command with { Handler = command.Handler! with { Code = command.Handler.Code! with { Code = body } } };
        var modeledEvent = _registerInvoice.Slice.Events.Single() with { Name = "EventForEventSourceId" };
        var slice = _processBatch with { Slice = _processBatch.Slice with { Commands = [command], Events = [modeledEvent] } };

        var error = Assert.Throws<CSharpImportRejected>(() => new StateChangeSliceRenderer().Render(slice, _applicationSet, "CratisApp"));

        Assert.Contains($"The inferred generated return type '{typeName}' is ambiguous between a modeled event type and a framework contract.", error.Message);
        Assert.Equal(body, slice.Slice.Commands.Single().Handler!.Code!.Code);
    }
}
