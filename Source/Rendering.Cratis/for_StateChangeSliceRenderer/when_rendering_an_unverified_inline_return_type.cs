// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_an_unverified_inline_return_type : state_change_slices
{
    RenderedFile _file = null!;

    void Establish()
    {
        var command = _processBatch.Slice.Commands.Single();
        command = command with { Handler = command.Handler! with { Code = command.Handler.Code! with { Code = "return new EventBatch();" } } };
        _processBatch = _processBatch with { Slice = _processBatch.Slice with { Commands = [command] } };
    }

    void Because() => _file = new StateChangeSliceRenderer().Render(_processBatch, _applicationSet, "CratisApp");

    [Fact] void should_keep_the_constructed_return_type() => _file.Content.ShouldContain("public EventBatch Handle(CommandContext context)");
    [Fact] void should_preserve_the_authored_body() => _file.Content.ShouldContain("return new EventBatch();");
    [Fact] void should_warn_about_collections_that_arc_cannot_append() => _file.Diagnostics.ShouldContainOnly([
        "Warning: Command 'ProcessBatch' handler's return type was inferred from unverified type 'EventBatch'. " +
        "If it is a collection that does not implement IEnumerable<object>, its events are not appended; return the events as IEnumerable<object> or a single event."]);
}
