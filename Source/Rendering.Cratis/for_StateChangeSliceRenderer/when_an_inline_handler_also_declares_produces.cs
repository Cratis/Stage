// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_an_inline_handler_also_declares_produces : state_change_slices
{
    Exception _error = null!;

    void Establish()
    {
        var command = _registerInvoice.Slice.Commands.Single() with { Handler = _processBatch.Slice.Commands.Single().Handler };
        _registerInvoice = _registerInvoice with { Slice = _registerInvoice.Slice with { Commands = [command] } };
    }

    void Because() => _error = Catch.Exception(() => new StateChangeSliceRenderer().Render(_registerInvoice, _applicationSet, "CratisApp"));

    [Fact] void should_reject_instead_of_falling_back_to_produces() => _error.ShouldBeOfExactType<UnsupportedInlineCommandHandler>();

    [Theory]
    [InlineData("Console.WriteLine(42);", "void")]
    [InlineData("return new List<object>();", "List<object>")]
    public void should_infer_an_independent_body_without_consulting_produces(string body, string returnType)
    {
        var command = _registerInvoice.Slice.Commands.Single();
        command = command with { Handler = command.Handler! with { Code = command.Handler.Code! with { Code = body } } };
        var slice = _registerInvoice with { Slice = _registerInvoice.Slice with { Commands = [command] } };

        var file = new StateChangeSliceRenderer().Render(slice, _applicationSet, "CratisApp");

        file.Content.ShouldContain($"public {returnType} Handle(CommandContext context)");
        file.Content.ShouldContain(body);
    }
}
