// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_an_inline_event_source_wrapper_return_type : state_change_slices
{
    [Theory]
    [InlineData("EventForEventSourceId")]
    [InlineData("global::EventForEventSourceId")]
    [InlineData("Cratis.Chronicle.EventSequences.EventForEventSourceId")]
    [InlineData("global::Cratis.Chronicle.EventSequences.EventForEventSourceId")]
    public void should_preserve_the_wrapper_type_without_warning(string typeName)
    {
        var command = _processBatch.Slice.Commands.Single();
        command = command with { Handler = command.Handler! with { Code = command.Handler.Code! with { Code = $"return new {typeName}();" } } };
        var slice = _processBatch with { Slice = _processBatch.Slice with { Commands = [command] } };

        var file = new StateChangeSliceRenderer().Render(slice, _applicationSet, "CratisApp");

        when_rendering_an_inline_return_type_declared_in_the_model.AssertReturnBinding(file, "Cratis.Chronicle.EventSequences.EventForEventSourceId");
        file.Content.ShouldContain(command.Handler.Code.Code);
        file.Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
    }
}
