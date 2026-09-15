// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_a_file_handler_also_declares_inline_code : a_file_handler_slice
{
    [Theory]
    [InlineData("return Array.Empty<object>();")]
    [InlineData("return new object[] { context.Identity.Id };")]
    public void should_reject_the_file_before_inline_admission_or_emission(string body)
    {
        // This mixed shape is a manual public model defense, not valid DSL.
        var code = new CodeBlockSyntax("csharp", body, SourceLocation.Start);
        var command = _command with { Handler = _command.Handler! with { Code = code } };
        var slice = _slice with { Slice = _slice.Slice with { Commands = [command] } };
        var error = Catch.Exception(() => new StateChangeSliceRenderer().Render(slice, _applicationSet, "CratisApp"));
        error.ShouldBeOfExactType<UnsupportedFileBackedCommandHandler>();
        Assert.Same(_command.Handler!.File, ((UnsupportedFileBackedCommandHandler)error).File);
    }
}
