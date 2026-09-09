// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_a_file_handler_is_not_the_first_command : a_file_handler_slice
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_block_the_selected_slice_before_emission_or_any_inline_guard(bool firstHandlerBindsContext)
    {
        var first = _command with
        {
            Name = "FirstCommand",
            Handler = firstHandlerBindsContext
                ? new HandlerSyntax(null, new("csharp", "return new object[] { context.Identity.Id };", SourceLocation.Start), SourceLocation.Start)
                : null,
            Produces = firstHandlerBindsContext ? [] : [new ProducesSyntax("BatchProcessed", null, [], SourceLocation.Start)],
        };
        var slice = _slice with { Slice = _slice.Slice with { Commands = [first, _command] } };
        var error = Catch.Exception(() => new StateChangeSliceRenderer().Render(slice, _applicationSet, "CratisApp"));
        error.ShouldBeOfExactType<UnsupportedFileBackedCommandHandler>();
        var rejection = (UnsupportedFileBackedCommandHandler)error;
        rejection.CommandName.ShouldEqual("ProcessBatch");
        rejection.FullSlicePath.ShouldEqual("Billing.Invoices.Process");
        rejection.CommandSourceLocation.ShouldEqual(_command.Location);
        Assert.Same(_command.Handler!.File, rejection.File);
    }
}
