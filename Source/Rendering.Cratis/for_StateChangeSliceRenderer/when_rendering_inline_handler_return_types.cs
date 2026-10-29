// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_inline_handler_return_types : state_change_slices
{
    [Theory]
    [InlineData("", "void")]
    [InlineData("Console.WriteLine(42);", "void")]
    [InlineData("return;", "void")]
    [InlineData("return new OrderPlaced();", "OrderPlaced")]
    [InlineData("yield return new OrderPlaced();", "IEnumerable<object>")]
    [InlineData("yield break;", "IEnumerable<object>")]
    [InlineData("var events = new List<object> { new OrderPlaced() }; return events;", "IEnumerable<object>")]
    [InlineData("return new List<object> { new OrderPlaced() };", "IEnumerable<object>")]
    [InlineData("return new System.Collections.Generic.List<object> { new OrderPlaced() };", "IEnumerable<object>")]
    [InlineData("return new HashSet<object> { new OrderPlaced() };", "IEnumerable<object>")]
    [InlineData("return new object[] { new OrderPlaced() };", "IEnumerable<object>")]
    public void should_emit_a_compiling_signature_and_preserve_the_body(string body, string returnType)
    {
        var command = _processBatch.Slice.Commands.Single();
        command = command with { Handler = command.Handler with { Code = command.Handler.Code with { Code = body } } };
        var placed = new EventSyntax("OrderPlaced", [], SourceLocation.Start);
        var slice = _processBatch with { Slice = _processBatch.Slice with { Commands = [command], Events = [placed] } };

        var file = new StateChangeSliceRenderer().Render(slice, _applicationSet, "CratisApp");

        file.Content.ShouldContain($"public {returnType} Handle(CommandContext context)");
        file.Content.ShouldContain(body);
        file.Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
        RenderedOutput.Errors([file]).ShouldBeEmpty();
    }
}
#endif
